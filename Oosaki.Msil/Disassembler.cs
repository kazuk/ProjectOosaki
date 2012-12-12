using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Oosaki.Msil.Extentions;

namespace Oosaki.Msil
{
    public class Disassembler
    {
        private static readonly Dictionary<short, OpCode> ByteOpcode;
        private static readonly Dictionary<short, OpCode> TwoByteOpcode;

        public DisassembledMethod Disassemble(Delegate method)
        {
            Contract.Requires(method != null);

            return Disassemble(method.Method);
        }

        [Pure]
        public bool CanDisassemble(MethodBase method)
        {
            return !method.IsAbstract 
                && (method.Attributes & MethodAttributes.PinvokeImpl) == 0
                && GetCodeType(method) == MethodImplAttributes.IL;
        }

        [Pure]
        public static MethodImplAttributes GetCodeType(MethodBase method)
        {
            var methodImplementationFlags = method.GetMethodImplementationFlags();
            if( methodImplementationFlags == MethodImplAttributes.InternalCall || 
                methodImplementationFlags == MethodImplAttributes.PreserveSig )
                return methodImplementationFlags;
            return (methodImplementationFlags & MethodImplAttributes.CodeTypeMask);
        }

        public DisassembledMethod Disassemble(MethodBase method)
        {
            Contract.Requires(method != null);
            Contract.Requires(CanDisassemble(method));

            var methodBody = method.GetMethodBody();
            if (methodBody == null)
            {
                throw new NotSupportedException(method.DeclaringType +" {" +  method.ToString() + "} "+ method.GetMethodImplementationFlags() );
            }
            Contract.Assert(methodBody!=null);
// ReSharper disable PossibleNullReferenceException
            var ilBytes = methodBody.GetILAsByteArray();
// ReSharper restore PossibleNullReferenceException

            Type returnType = method is MethodInfo
                                  ? (method as MethodInfo).ReturnType
                                  : typeof (void);

            var handlingClauses = methodBody.ExceptionHandlingClauses;
            Type[] genericParameters = null;
            if (method.IsGenericMethod)
            {
                genericParameters = method.GetGenericArguments();
            }
            return new DisassembledMethod(
                ilBytes,
                Scan(ilBytes, handlingClauses),
                new ModuleMetadataResolver(method.Module),
                handlingClauses, 
                methodBody.LocalVariables,
                returnType,
                method.GetParameters(),
                method.IsGenericMethodDefinition ,
                genericParameters );
        }

        private IEnumerable<int> Scan(byte[] ilBytes, IList<ExceptionHandlingClause> exceptionHandlingClauses)
        {
            IEnumerable<int> ilOffsets = ScanRawIl(ilBytes).ToList();
            var branches = new Dictionary<int, List<int>>();

            Action<int, int> addBranches = (source, target) =>
                {
                    List<int> sourceList;
                    if (branches.TryGetValue(target, out sourceList))
                    {
                        sourceList.Add(source);
                    }
                    else
                    {
                        branches.Add(target, new List<int>(source));
                    }
                };

            foreach (var ilOffset in ilOffsets)
            {
                OpCode opCode = GetOpCodeFrom(ilBytes, ilOffset);
                if (opCode.Value == OpCodes.Switch.Value)
                {
                    int count = GetOperandSwitchCount(ilBytes, ilOffset, opCode);
                    int beginBrList = ilOffset + opCode.Size + 4;
                    int brSource = beginBrList + count*4;
                    for (int index = 0; index < count; index ++)
                    {
                        int brTarget = brSource + ilBytes.ReadInt32(beginBrList + index*4);
                        addBranches(ilOffset, brTarget);
                    }
                }
                else if (opCode.OperandType == OperandType.InlineBrTarget ||
                    opCode.OperandType == OperandType.ShortInlineBrTarget)
                {
                    int brTarget = GetOperandBrTarget(ilBytes, ilOffset, opCode);
                    addBranches( ilOffset,brTarget);
                }
            }

            foreach (var ilOffset in ilOffsets)
            {
                foreach (var exceptionHandlingIndex in GetTryBlockIndices(exceptionHandlingClauses, ilOffset))
                {
                    yield return NonIlOffsets.BeginTryBlock(exceptionHandlingIndex);
                }

                for (int index = 0; index < exceptionHandlingClauses.Count; index++)
                {
                    var exceptionHandlingClause = exceptionHandlingClauses[index];
                    if (exceptionHandlingClause.TryOffset + exceptionHandlingClause.TryLength==ilOffset)
                    {
                        yield return NonIlOffsets.EndTryBlock(index);
                    }
                    if (exceptionHandlingClause.HandlerOffset == ilOffset)
                    {
                        if( exceptionHandlingClause.Flags == ExceptionHandlingClauseOptions.Clause ) 
                            yield return NonIlOffsets.BeginCatchBlock(index);
                        if (exceptionHandlingClause.Flags == ExceptionHandlingClauseOptions.Fault)
                            yield return NonIlOffsets.BeginCatchBlock(index);
                        if( exceptionHandlingClause.Flags == ExceptionHandlingClauseOptions.Finally )
                            yield return NonIlOffsets.BeginFinallyBlock(index);
                    }
                    if (exceptionHandlingClause.HandlerOffset + exceptionHandlingClause.HandlerLength == ilOffset)
                    {
                        yield return NonIlOffsets.EndHandlerBlock(index);
                    }
                    if( exceptionHandlingClause.Flags == ExceptionHandlingClauseOptions.Filter 
                        && exceptionHandlingClause.FilterOffset==ilOffset)
                    {
                        yield return NonIlOffsets.BeginFilterBlock(index);
                    }
                }

                List<int> brSource;
                if (branches.TryGetValue(ilOffset, out brSource))
                {
                    yield return NonIlOffsets.Label( ilOffset );
                }

                yield return ilOffset;
            }
        }

        private static IEnumerable<int> GetTryBlockIndices(IEnumerable<ExceptionHandlingClause> exceptionHandlingClauses, int ilOffset)
        {
            return exceptionHandlingClauses
                .Select( (clause,index)=> new {clause,index} )
                .Where( c=> c.clause.TryOffset == ilOffset )
                .OrderByDescending( c=>c.clause.TryLength )
                .Select( r=>r.index);
        }

        private int GetOperandSwitchCount(byte[] ilBytes, int ilOffset, OpCode opCode)
        {
            Contract.Requires(opCode.OperandType==OperandType.InlineSwitch);
            return ilBytes.ReadInt32(ilOffset + opCode.Size);
        }

        private int GetOperandBrTarget(byte[] ilBytes, int ilOffset, OpCode opCode)
        {
            Contract.Requires( opCode.OperandType== OperandType.InlineBrTarget || 
                opCode.OperandType==OperandType.ShortInlineBrTarget);

            if (opCode.OperandType == OperandType.InlineBrTarget)
            {
                var brOffset = ilBytes.ReadInt32(ilOffset + opCode.Size);
                int brSource = ilOffset + opCode.Size + 4 /* operand size */;
                return brSource + brOffset;
            }
            if (opCode.OperandType == OperandType.ShortInlineBrTarget)
            {
                int brOffset = (sbyte) ilBytes[ilOffset + opCode.Size];
                int brSource = ilOffset + opCode.Size + 1 /* operand size */;
                return brSource + brOffset;
            }
            throw new NotSupportedException("required opCode.OperandType== OperandType.InlineBrTarget || opCode.OperandType==OperandType.ShortInlineBrTarget");
        }

        private OpCode GetOpCodeFrom(byte[] ilBytes, int ilOffset)
        {
            byte b = ilBytes[ilOffset];
            OpCode opCode;
            if (!TryGetOpCode(b, out opCode))
            {
                var s = (short) ( (b<<8) | ilBytes[ilOffset+1]);
                if (!TryGetOpCode(s, out opCode))
                {
                    throw new NotSupportedException("unknown IL opcode at offset:" + ilOffset);
                }
            }
            return opCode;
        }

        private IEnumerable<int> ScanRawIl(byte[] ilBytes)
        {
            for (int offset = 0; offset < ilBytes.Length; )
            {
                OpCode opCode = GetOpCodeFrom(ilBytes, offset);

                yield return offset;

                offset += opCode.Size;
                int operandSize = 0;

                if (opCode.OperandType == OperandType.InlineBrTarget
                    || opCode.OperandType == OperandType.InlineField
                    || opCode.OperandType == OperandType.InlineI
                    || opCode.OperandType == OperandType.InlineMethod
                    || opCode.OperandType == OperandType.InlineSig
                    || opCode.OperandType == OperandType.InlineString
                    || opCode.OperandType == OperandType.InlineTok
                    || opCode.OperandType == OperandType.InlineType
                    || opCode.OperandType == OperandType.ShortInlineR
                    )
                {
                    operandSize = 4;
                }
                else if (
                         opCode.OperandType == OperandType.ShortInlineBrTarget
                         || opCode.OperandType == OperandType.ShortInlineI
                         || opCode.OperandType == OperandType.ShortInlineVar)
                {
                    operandSize = 1;
                }
                else if (opCode.OperandType == OperandType.InlineR ||
                    opCode.OperandType == OperandType.InlineI8)
                {
                    operandSize = 8;
                }
                else if (opCode.OperandType == OperandType.InlineVar)
                {
                    operandSize = 2;
                }

                if (opCode.OperandType != OperandType.InlineSwitch)
                {
                    offset += operandSize;
                }
                else
                {
                    int count = ilBytes.ReadInt32(offset);
                    offset += 4 + 4 * count;
                }
            }
        }

        static Disassembler()
        {
            var opCodes = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
                                         .Where(f => f.FieldType == typeof(OpCode))
                                         .Select(f => (OpCode)f.GetValue(null))
                                         .Where( op=>op.OpCodeType != OpCodeType.Nternal)
                                         .ToList();

            ByteOpcode = opCodes.Where(op => op.Size == 1).ToDictionary(op => op.Value);
            TwoByteOpcode = opCodes.Where(op => op.Size == 2).ToDictionary(op => op.Value);
        }

        internal static bool TryGetOpCode(byte opCodeValue, out OpCode opCode)
        {
            return ByteOpcode.TryGetValue(opCodeValue, out opCode);
        }

        internal static bool TryGetOpCode(short opCodeValue, out OpCode opCode)
        {
            return TwoByteOpcode.TryGetValue(opCodeValue, out opCode);
        }

    }
}