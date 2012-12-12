using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oosaki.Msil.Extentions;

namespace Oosaki.Msil.Samples
{
    /// <summary>
    /// Disassembler Samples
    /// </summary>
    [TestClass]
    public class DisassemblerSamples
    {
        public TestContext TestContext { get; set; }


        // this test shows Disassembling the method , and IsCalling(MethodInfo) works
        [TestMethod]
        public void DisassembleThisMethodAndCheckCalling()
        {
            Action thisMethod = DisassembleThisMethodAndCheckCalling;
            var disassembler = new Disassembler();

            // disassembling this method
            var result = disassembler.Disassemble(thisMethod);
            result.IsNotNull();

            // tests this method calls Disassembler.Disassemble
            var method = ReflectionEx.GetMethod<Action>(() => disassembler.Disassemble(thisMethod));
            result.IsCalling(method).IsTrue();

            // this method not calling Console.WriteLine
            var cwr = ReflectionEx.GetMethod<Action>(() =>  Console.WriteLine());
            result.IsCalling(cwr).IsFalse();
        }

        private void ControlFlowLabels()
        {
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine(i);
            }
        }

        [TestMethod]
        public void ControlFlowLabelsSample()
        {
            Action method = ControlFlowLabels;
            var disassembler = new Disassembler();
            var result = disassembler.Disassemble(method);
            result.IsNotNull();

            // enumerate label with Labels result count is 2
            //
            // because for (int i = 0; i < 100; i++) { WriteLine()} is compiled as
            // i=0; goto Label1;
            // Label0: Console.WriteLine(i);
            // i++;
            // Label1: if( (i<100 ) ) goto Label0;
            // 
            result.Labels.Count().Is(2);

        }


        private void ExceptionFlow()
        {
            try
            {
                Console.WriteLine("trying");
            }
            catch (Exception )
            {
                Console.WriteLine("catched");
            }
            finally
            {
                Console.WriteLine("finally");
            }
            
        }

        // this method shows an exception flow anasisys 
        [TestMethod]
        public void ExceptionFlowAnalisys()
        {
            Action target = ExceptionFlow;
            var disassembler = new Disassembler();

            var result = disassembler.Disassemble(target);

            // TryBlocks returns 2 element and first is Finally - because compiled as try { try {} catch{} } finally {}
            result.TryBlocks.Count().Is(2);
            result.TryBlocks.First().Flags.Is(ExceptionHandlingClauseOptions.Finally);

            // catch and finallies is 1
            result.CatchBlocks.Count().Is(1);
            result.FinallyBlocks.Count().Is(1);

        }
    }
}
