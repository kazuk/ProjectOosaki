using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Oosaki.Msil.Samples
{
    // this tests shows Disassemble to Assemble the method and class
    [TestClass]
    public class ReassembleAndTransformSamples
    {

        private static void ReassembleSample()
        {
            Trace.WriteLine("run reassembled!");
        }

        // disassemble and assemble the simple method
        [TestMethod]
        public void ReassembleSimpleMethod()
        {
            var disassembler = new Disassembler();
            var result = disassembler.Disassemble((Action) ReassembleSample);

            using (var assembleContext = new AssembleContext())
            {
                var assembled = result.Assemble(assembleContext);

                assembled.Invoke(null, new object[] {});

                // reassembled code is not in this class
                assembled.DeclaringType.IsNot(GetType());
            }
        }

        private static void UsesDateTimeNow()
        {
            Trace.WriteLine( DateTime.Now );
        }

        public class DateTimeFake
        {
            public static DateTime Now
            {
                get
                {
                   return new DateTime(2012,12,25);
                }
            }
        }

        [TestMethod]
        public void ReassembleAndTransformWithMethodMap()
        {
            var disassembler = new Disassembler();
            var result = disassembler.Disassemble((Action) UsesDateTimeNow);

            using (var assembleContext = new AssembleContext())
            {
                // maps DateTime.get_Now to DateTimeFake.get_Now
                result.MapMethod(
                    typeof (DateTime).GetProperty("Now").GetMethod,
                    typeof (DateTimeFake).GetProperty("Now").GetMethod);

                var assembled = result.Assemble(assembleContext);
                assembled.Invoke(null, new object[] {});
            }
        }
    }


}
