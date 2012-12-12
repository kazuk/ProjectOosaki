using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oosaki.Msil.Extentions;

namespace Oosaki.Msil.Tests
{
    [TestClass]
    public class DisassemblerTests
    {
        // do disasssemble System.dll 
        //  all types 
        [TestMethod]
        public void DisassembleSystemAll()
        {
            foreach (var type in typeof( Uri).Assembly.GetTypes() )
            {
                foreach (var member in type.GetAllMembers())
                {
                    if (member is MethodInfo)
                    {
                        var method = member as MethodInfo;
                        var disassembler = new Disassembler();
                        if (disassembler.CanDisassemble(method))
                        {
                            disassembler.Disassemble(method);
                        }
                    }
                    if (member is ConstructorInfo)
                    {
                        var ctor = member as ConstructorInfo;
                        var disassembler = new Disassembler();
                        if (disassembler.CanDisassemble(ctor))
                        {
                            disassembler.Disassemble(ctor);
                        }

                    }
                }
            }
        }
        // do disasssemble System.dll 
        //  all types 
        [TestMethod]
        public void ReassembleSystemAll()
        {
            foreach (var type in typeof(Uri).Assembly.GetTypes())
            {
                if( type.IsGenericType ) continue;
                foreach (var member in type.GetAllMembers())
                {
                    try
                    {
                        if (member is MethodInfo)
                        {
                            ReAssembleMethod(member as MethodInfo);
                        }
                        if (member is ConstructorInfo)
                        {
                            ReAssembleMethod(member as ConstructorInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException( type + " " + member + " ReAssemble failed",ex );
                    }
                }
            }
        }

        [TestMethod]
        public void ReassembleFail()
        {
            foreach (var type in typeof(Uri).Assembly.GetTypes())
            {
                if (type.Name != "ReflectEventDescriptor") continue;
                foreach (var member in type.GetAllMembers())
                {
                    if (member is MethodInfo)
                    {
                        ReAssembleMethod(member as MethodInfo);
                    }
                    if (member is ConstructorInfo)
                    {
                        ReAssembleMethod(member as ConstructorInfo);
                    }
                }
                
            }
        }

        [TestMethod]
        public void ReassembleFail2()
        {
            foreach (var type in typeof(Uri).Assembly.GetTypes())
            {
                if (type.Name != "ReflectPropertyDescriptor") continue;
                foreach (var member in type.GetAllMembers())
                {
                    if (member is MethodInfo)
                    {
                        ReAssembleMethod(member as MethodInfo);
                    }
                    if (member is ConstructorInfo)
                    {
                        ReAssembleMethod(member as ConstructorInfo);
                    }
                }

            }
        }


        private static void ReAssembleMethod(MethodBase method)
        {
            var disassembler = new Disassembler();
            if (disassembler.CanDisassemble(method))
            {
                var result = disassembler.Disassemble(method);
                result.Assemble(new AssembleContext());
            }
        }
    }
}
