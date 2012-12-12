using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Oosaki.Msil.Extentions;

namespace Oosaki.Msil
{
    public class DisassembledMethod
    {
        private readonly IEnumerable<int> _ilOffsets;
        private readonly byte[] _ilbytes;
        private readonly IMetadataTokenResolver _resolver;
        private readonly IList<ExceptionHandlingClause> _exceptionHandlingClauses;
        private readonly IList<LocalVariableInfo> _localVariables;

        internal DisassembledMethod(byte[] ilbytes, IEnumerable<int> ilOffsets,
            IMetadataTokenResolver resolver, 
            IList<ExceptionHandlingClause> exceptionHandlingClauses, 
            IList<LocalVariableInfo> localVariables, Type returnType, ParameterInfo[] parameters, bool isGenericMethod, Type[] genericParameters)
        {
            Contract.Requires(ilbytes != null);
            Contract.Requires(ilOffsets != null);
            Contract.Requires(resolver!=null);
            Contract.Requires(exceptionHandlingClauses!=null);
            Contract.Requires(localVariables!=null);

            _ilbytes = ilbytes;
            _resolver = resolver;
            _exceptionHandlingClauses = exceptionHandlingClauses;
            _localVariables = localVariables;
            _returnType = returnType;
            _parameters = parameters;
            _isGenericMethod = isGenericMethod;
            _genericParameters = genericParameters;
            _ilOffsets = new List<int>(ilOffsets);
        }

        /// <summary>
        /// ラベルおよび、制御フローマーカーを除去し、単純IL命令列毎のグループに
        /// 分割します。
        /// </summary>
        public IEnumerable<IEnumerable<int>> ControlFlows
        {
            get
            {
                var flows = new List<IEnumerable<int>>();
                List<int> currentFlow = null;
                foreach (var ilOffset in _ilOffsets)
                {
                    if (IsPureOffset(ilOffset))
                    {
                        if (currentFlow == null)
                        {
                            currentFlow = new List<int>();
                            flows.Add(currentFlow);
                        }
                        currentFlow.Add(ilOffset);
                    }
                    else
                    {
                        currentFlow = null;
                    }
                }
                return flows;
            }
        }

        public IEnumerable<ExceptionHandlingClause> TryBlocks
        {
            get
            {
                foreach (var ilOffset in _ilOffsets.Where(ilOffset => !IsPureOffset(ilOffset )))
                {
                    ExceptionHandlingClause exceptionHandlingClause;
                    if( IsTryBlockBegin( ilOffset,out exceptionHandlingClause ) )
                    {
                        yield return exceptionHandlingClause;
                    }
                }
            }
        }
        public IEnumerable<ExceptionHandlingClause> CatchBlocks
        {
            get
            {
                foreach (var ilOffset in _ilOffsets.Where(ilOffset => !IsPureOffset(ilOffset)))
                {
                    ExceptionHandlingClause exceptionHandlingClause;
                    if (IsCatchBlockBegin(ilOffset, out exceptionHandlingClause))
                    {
                        yield return exceptionHandlingClause;
                    }
                }
            }
        }

        public IEnumerable<ExceptionHandlingClause> FinallyBlocks
        {
            get
            {
                foreach (var ilOffset in _ilOffsets.Where(ilOffset => !IsPureOffset(ilOffset)))
                {
                    ExceptionHandlingClause exceptionHandlingClause;
                    if (IsFinallyBlockBegin(ilOffset, out exceptionHandlingClause))
                    {
                        yield return exceptionHandlingClause;
                    }
                }
            }

        }

        public IEnumerable<int> Labels
        {
            get
            {
                foreach (var ilOffset in _ilOffsets.Where(ilOffset=> !IsPureOffset(ilOffset)))
                {
                    int id;
                    if (IsLabel(ilOffset,out id))
                    {
                        yield return ilOffset;
                    }
                }
            }
        }


        public static bool IsLabel(int ilOffset, out int id)
        {
            return NonIlOffsets.IsLabel(ilOffset, out id);
        }

        public bool IsFinallyBlockBegin(int ilOffset, out ExceptionHandlingClause exceptionHandlingClause)
        {
            exceptionHandlingClause = null;
            int handlingClauseIndex;
            if (NonIlOffsets.IsFinallyBlockBegin(ilOffset, out handlingClauseIndex))
            {
                exceptionHandlingClause = _exceptionHandlingClauses[handlingClauseIndex];
                return true;
            }
            return false;
        }

        private bool IsFinallyBlockEnd(int ilOffset, out ExceptionHandlingClause exceptionHandlingClause)
        {
            exceptionHandlingClause = null;
            int handlingClauseIndex;
            if (NonIlOffsets.IsFinallyBlockEnd(ilOffset, out handlingClauseIndex))
            {
                exceptionHandlingClause = _exceptionHandlingClauses[handlingClauseIndex];
                return true;
            }
            return false;
        }


        public bool IsCatchBlockBegin(int ilOffset, out ExceptionHandlingClause exceptionHandlingClause)
        {
            exceptionHandlingClause = null;
            int handlingClauseIndex;
            if (NonIlOffsets.IsCatchBlockBegin(ilOffset, out handlingClauseIndex))
            {
                exceptionHandlingClause = _exceptionHandlingClauses[handlingClauseIndex];
                return true;
            }
            return false;
        }
        private bool IsCatchBlockEnd(int ilOffset, out ExceptionHandlingClause exceptionHandlingClause)
        {
            exceptionHandlingClause = null;
            int handlingClauseIndex;
            if (NonIlOffsets.IsCatchBlockEnd(ilOffset, out handlingClauseIndex))
            {
                exceptionHandlingClause = _exceptionHandlingClauses[handlingClauseIndex];
                return true;
            }
            return false;
        }

        public bool IsTryBlockBegin(int ilOffset, out ExceptionHandlingClause exceptionHandlingClause)
        {
            exceptionHandlingClause = null;
            int handlingClauseIndex;
            if (NonIlOffsets.IsTryBlockBegin(ilOffset, out handlingClauseIndex))
            {
                exceptionHandlingClause = _exceptionHandlingClauses[handlingClauseIndex];
                return true;
            }
            return false;
        }

        private bool IsTryBlockEnd(int ilOffset, out ExceptionHandlingClause exceptionHandlingClause)
        {
            exceptionHandlingClause = null;
            int handlingClauseIndex;
            if (NonIlOffsets.IsTryBlockEnd(ilOffset, out handlingClauseIndex))
            {
                exceptionHandlingClause = _exceptionHandlingClauses[handlingClauseIndex];
                return true;
            }
            return false;
        }

        private IEnumerable<MethodBase> CallingMethods()
        {
            return from ilOffset in _ilOffsets.Where(IsPureOffset)
                   let opCode = GetOpCode(ilOffset)
                   where opCode.Value == OpCodes.Call.Value || opCode.Value == OpCodes.Callvirt.Value
                   select _resolver.ResolveMethod(GetMethodToken(opCode, ilOffset));
        }

        public bool IsCalling(MethodInfo method)
        {
            return CallingMethods().Contains(method);
        }

        private int GetMethodToken(OpCode opCode, int ilOffset)
        {
            Contract.Requires(ilOffset + opCode.Size + 4 < _ilbytes.Length);

            return _ilbytes.ReadInt32(ilOffset + opCode.Size);
        }

        private OpCode GetOpCode(int ilOffset)
        {
            OpCode opCode;
            var b = _ilbytes[ilOffset];
            if (!Disassembler.TryGetOpCode(b, out opCode))
            {
                var s = (short) ((b << 8) | _ilbytes[ilOffset + 1]);
                if (!Disassembler.TryGetOpCode(s, out opCode))
                {
                    throw new OpCodeValueNotValidException();
                }
            }
            return opCode;
        }

        public bool IsPureOffset(int ilOffset)
        {
            return ilOffset >= 0;
        }

        private Type _returnType;
        private ParameterInfo[] _parameters;
        private readonly bool _isGenericMethod;
        private readonly Type[] _genericParameters;


        public MethodInfo Assemble(AssembleContext assembleContext)
        {
            var assemblyBuilder = assembleContext.GetAssemblyBuilder();
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DefaultDynamicModule");
            var typeBuilder = moduleBuilder.DefineType("DefaultTypeName");

            var methodBuilder = typeBuilder.DefineMethod("DefualtMethodName", 
                MethodAttributes.Public | MethodAttributes.Static ,
                _returnType,
                _parameters.Select(p=>p.ParameterType).ToArray()
                 );
            if (_isGenericMethod)
            {
                var genericParameterBuilder = methodBuilder.DefineGenericParameters(_genericParameters.Select(t => t.Name).ToArray());
/*
                foreach (var builders in _genericParameters.Select((t, n) => new
                    {
                       Type= t, Builder= genericParameterBuilder[n]
                    }))
                {
                    var builder = builders.Builder;
                }
  */              

            }

            var paramMap = BuildParameterMap(methodBuilder);
            var ilgenerator = methodBuilder.GetILGenerator();
            var localMap = _localVariables.ToDictionary(
                local => local.LocalIndex, 
                local => ilgenerator.DeclareLocal(local.LocalType, local.IsPinned));

            var labels = new Dictionary<int, Label>();
            var ilOffsets = _ilOffsets.ToArray();
            foreach (var ilOffset in ilOffsets.Where(ilOffset => !IsPureOffset(ilOffset)))
            {
                int id;
                if (IsLabel(ilOffset,out id))
                {
                    var label = ilgenerator.DefineLabel();
                    labels.Add(id,label);
                }
            }
            for (int index = 0; index < ilOffsets.Length; index++)
            {
                var ilOffset = ilOffsets[index];
                if (IsPureOffset(ilOffset))
                {
                    Debug.WriteLine(ilgenerator.ILOffset + ":" + ilOffset);

                    OpCode opCode = GetOpCode(ilOffset);
                    Debug.WriteLine(opCode);
                    // this opCode write by EndExceptionBlock etc
                    if (opCode == OpCodes.Leave_S || opCode == OpCodes.Leave)
                    {
                        continue;
                        ExceptionHandlingClause excep;
                        var nextIlOffset = ilOffsets[index + 1];
                        if (IsTryBlockEnd(nextIlOffset, out excep)) continue;
                        if (IsCatchBlockEnd(nextIlOffset, out excep)) continue;
                        if (IsFinallyBlockEnd(nextIlOffset, out excep)) continue;
                    }

                    switch (opCode.OperandType)
                    {
                        case OperandType.InlineNone:
                            ilgenerator.Emit(opCode);
                            break;
                        case OperandType.InlineString:
                            ilgenerator.Emit(opCode, GetOperandString(opCode, ilOffset));
                            break;
                        case OperandType.InlineMethod:
                            ilgenerator.Emit(opCode, GetOperandMethod(opCode, ilOffset));
                            break;
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.InlineBrTarget:
                            ilgenerator.Emit(opCode, labels[GetOperandLabel(opCode, ilOffset)]);
                            break;
                        case OperandType.InlineType:
                            ilgenerator.Emit(opCode, GetOperandType(opCode, ilOffset));
                            break;
                        case OperandType.InlineField:
                            ilgenerator.Emit(opCode, GetOperandFiled(opCode, ilOffset));
                            break;
                        case OperandType.ShortInlineVar:
                            if (opCode.IsParameterAccess())
                            {
                                ilgenerator.Emit(opCode, (byte) paramMap[GetOperandParameter(opCode, ilOffset)].Position);
                            }
                            else
                            {
                                ilgenerator.Emit(opCode, localMap[GetOperandVar(opCode, ilOffset)]);
                            }
                            break;
                        case OperandType.ShortInlineI:
                            ilgenerator.Emit(opCode, GetOperandByte(opCode, ilOffset));
                            break;
                        case OperandType.InlineI:
                            ilgenerator.Emit(opCode, GetOperandInt(opCode, ilOffset));
                            break;
                        case OperandType.InlineR:
                            ilgenerator.Emit(opCode, GetOperandDouble(opCode, ilOffset));
                            break;
                        case OperandType.InlineSwitch:
                            ilgenerator.Emit(opCode, GetOperandSwitchLabels(opCode, ilOffset)
                                                         .Select(id => labels[id]).ToArray());
                            break;
                        case OperandType.InlineTok:
                            var tokenValue = GetOperandTokenValue(opCode, ilOffset);
                            switch (ToTokenType(tokenValue))
                            {
                                case TokenType.MethodToken:
                                    ilgenerator.Emit(opCode, _resolver.ResolveMethod(tokenValue));
                                    break;
                                case TokenType.TypeToken:
                                case TokenType.TypeToken2:
                                    ilgenerator.Emit(opCode, _resolver.ResolveType(tokenValue));
                                    break;
                                case TokenType.FieldToken:
                                    ilgenerator.Emit(opCode, _resolver.ResolveField(tokenValue));
                                    break;
                                case TokenType.SigToken:
                                    var signature = _resolver.ResolveSignature(tokenValue);
                                    var sigToken = moduleBuilder.GetSignatureToken(signature, signature.Length);
                                    ilgenerator.Emit(opCode, sigToken.Token);
                                    break;
                                default:
                                    throw new NotImplementedException(tokenValue.ToString("x8"));
                            }
                            break;
                        default:
                            throw new NotImplementedException("まだ書いてない?" + opCode.OperandType.ToString());
                    }
                }
                else
                {
                    int id;
                    if (IsLabel(ilOffset, out id))
                    {
                        ilgenerator.MarkLabel(labels[id]);
                        continue;
                    }
                    ExceptionHandlingClause handling;
                    if (IsTryBlockBegin(ilOffset, out handling))
                    {
                        ilgenerator.BeginExceptionBlock();
                        continue;
                    }
                    if (IsTryBlockEnd(ilOffset, out handling))
                    {
                        continue;
                    }
                    if (IsCatchBlockBegin(ilOffset, out handling))
                    {
                        ilgenerator.BeginCatchBlock(handling.CatchType);
                        continue;
                    }
                    if (IsCatchBlockEnd(ilOffset, out handling))
                    {
                        ilgenerator.EndExceptionBlock();
                        continue;
                    }
                    if (IsFinallyBlockBegin(ilOffset, out handling))
                    {
                        ilgenerator.BeginFinallyBlock();
                        continue;
                    }
                    if (IsFinallyBlockEnd(ilOffset, out handling))
                    {
                        ilgenerator.EndExceptionBlock();
                        continue;
                    }
                    throw new NotImplementedException("まだ書いてない？");
                }
            }
            var type= typeBuilder.CreateType();
            return type.GetMethod( methodBuilder.Name );
        }

        private SignatureHelper ToSignatureHelper(byte[] signature)
        {
            if (signature[0] == 0x1d)
            {
                var sigHelper = SignatureHelper.GetLocalVarSigHelper();
                sigHelper.GetSignature();
            }
            throw new NotImplementedException();
        }


        enum TokenType : byte
        {
            TypeToken = 0x01,
            TypeToken2 = 0x02,
            FieldToken = 0x04,
            MethodToken = 0x06,
            SigToken = 0x1b

        }

        private TokenType ToTokenType(int tokenValue)
        {
            return (TokenType) ((byte) (tokenValue >> 24));
        }

        private int GetOperandTokenValue(OpCode opCode, int ilOffset)
        {
            return _ilbytes.ReadInt32(ilOffset + opCode.Size);
        }

        private Type GetOperandToken(OpCode opCode, int ilOffset)
        {
            int token = _ilbytes.ReadInt32(ilOffset + opCode.Size);
            return _resolver.ResolveTokenAsType(token);
        }

        private IEnumerable<int> GetOperandSwitchLabels(OpCode opCode, int ilOffset)
        {
            int count = _ilbytes.ReadInt32(ilOffset + opCode.Size);
            var result = new int[count];
            var targetsOffset = ilOffset + opCode.Size + 4;
            var brSource = targetsOffset + count*4;
            for (int i = 0; i < count; i++)
            {
                result[i] = brSource+ _ilbytes.ReadInt32( targetsOffset + i*4 );
            }
            return result;
        }

        private double GetOperandDouble(OpCode opCode, int ilOffset)
        {
            return BitConverter.ToDouble(_ilbytes, ilOffset + opCode.Size);
        }

        private int GetOperandInt(OpCode opCode, int ilOffset)
        {
            return _ilbytes.ReadInt32(ilOffset + opCode.Size);
        }

        private byte GetOperandByte(OpCode opCode, int ilOffset)
        {
            return _ilbytes[ilOffset + opCode.Size];
        }

        private int GetOperandParameter(OpCode opCode, int ilOffset)
        {
            if (opCode.OperandType == OperandType.ShortInlineVar)
            {
                return _ilbytes[ilOffset + opCode.Size];
            }
            throw new NotImplementedException();
        }

        private Dictionary<int, ParameterBuilder> BuildParameterMap(MethodBuilder methodBuilder)
        {
            var paramAndResults = new List<ParameterBuilder>();
            if (_returnType != typeof (void))
            {
                paramAndResults.Add(methodBuilder.DefineParameter(0, ParameterAttributes.Retval, null));
            }
            int paramPos = 1;
            foreach (var parameter in _parameters)
            {
                var paramBuilder = methodBuilder.DefineParameter(paramPos, ParameterAttributes.None, parameter.Name);

                paramAndResults.Add(paramBuilder);
                paramPos++;
            }
            var paramMap = paramAndResults.ToDictionary(p => p.Position);
            return paramMap;
        }

        private int GetOperandVar(OpCode opCode, int ilOffset)
        {
            if (opCode.OperandType == OperandType.ShortInlineVar)
            {
                return _ilbytes[ilOffset + opCode.Size];
            }
            throw new NotImplementedException();
        }

        private FieldInfo GetOperandFiled(OpCode opCode, int ilOffset)
        {
            int token = _ilbytes.ReadInt32(ilOffset + opCode.Size);
            return _resolver.ResolveField(token);
        }

        private Type GetOperandType(OpCode opCode, int ilOffset)
        {
            int token = _ilbytes.ReadInt32(ilOffset + opCode.Size);
            return _resolver.ResolveType(token);
        }

        private int GetOperandLabel(OpCode opCode, int ilOffset)
        {
            if (opCode.OperandType == OperandType.ShortInlineBrTarget)
            {
                var brOffset =(sbyte) _ilbytes[ilOffset + opCode.Size];
                return ilOffset + opCode.Size + 1 /*sizeof(byte)*/ + brOffset;
            }
            if (opCode.OperandType == OperandType.InlineBrTarget)
            {
                var brOffset = _ilbytes.ReadInt32(ilOffset + opCode.Size);
                return ilOffset + opCode.Size + 4 /*sizeof(byte)*/ + brOffset;
            }
            throw new ApplicationException("まだ書いてない");
        }

        private MethodBase GetOperandMethod(OpCode opCode, int ilOffset)
        {
            int token = _ilbytes.ReadInt32(ilOffset + opCode.Size);
            return _resolver.ResolveMethod(token);
        }

        private string GetOperandString(OpCode opCode, int ilOffset)
        {
            int token = _ilbytes.ReadInt32(ilOffset + opCode.Size);
            return _resolver.ResolveString( token);
        }

        public void MapType(Type fromType, Type toType)
        {
            _resolver.MapType(fromType, toType);
        }

        public void MapMethod(MethodInfo fromMethod, MethodInfo toMethod)
        {
            _resolver.MapMethod(fromMethod, toMethod);
        }
    }
}