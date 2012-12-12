using System.Reflection;
using System.Reflection.Emit;

namespace Oosaki.Msil.Extentions
{
    public static class IlGeneratorExtention
    {
        public static void Emit(this ILGenerator ilGenerator,OpCode opCode, MethodBase methodBase)
        {
            var methodInfo = methodBase as MethodInfo;
            if (methodInfo != null)
            {
                ilGenerator.Emit(opCode, methodInfo);
            }
            else
            {
                var ctorInfo = methodBase as ConstructorInfo;
                if (ctorInfo != null)
                {
                    ilGenerator.Emit(opCode, ctorInfo);
                }
            }
        }
    }
}
