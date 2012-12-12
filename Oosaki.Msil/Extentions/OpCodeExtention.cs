using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Oosaki.Msil.Extentions
{
    public static class OpCodeExtention
    {
        private static readonly IEnumerable<short> ParameterAccessOpCodes = new[]
            {
                OpCodes.Ldarg.Value,
                OpCodes.Ldarg_0.Value,
                OpCodes.Ldarg_1.Value,
                OpCodes.Ldarg_2.Value,
                OpCodes.Ldarg_3.Value,
                OpCodes.Ldarg_S.Value,
                OpCodes.Ldarga.Value,
                OpCodes.Ldarga_S.Value,
                OpCodes.Starg.Value,
                OpCodes.Starg_S.Value,
                OpCodes.Arglist.Value
            };

        public static bool IsParameterAccess(this OpCode opCode)
        {
            return ParameterAccessOpCodes.Contains(opCode.Value);
        }
    }
}
