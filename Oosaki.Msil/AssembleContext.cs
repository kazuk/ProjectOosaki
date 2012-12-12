using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Oosaki.Msil
{
    public class AssembleContext : IDisposable
    {
        private string _assemblyName;
        private AssemblyBuilder _assemblyBuilder;
        private AssemblyBuilderAccess _assemblyBuilderAccess = AssemblyBuilderAccess.RunAndSave;

        public AssembleContext() : this( "DefaultAssemblyName" )
        {
        }

        public AssembleContext(string assemblyName)
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            _assemblyName = assemblyName;
        }

        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name == _assemblyName)
            {
                return _assemblyBuilder;
            }
            return null;
        }

        public AssemblyBuilderAccess AssemblyBuilderAccess
        {
            get { return _assemblyBuilderAccess; }
            set { _assemblyBuilderAccess = value; }
        }

        public AssemblyBuilder GetAssemblyBuilder()
        {
            if (_assemblyBuilder == null)
            {
                AssemblyBuilderAccess = AssemblyBuilderAccess.RunAndCollect;
                _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName(_assemblyName),
                        AssemblyBuilderAccess
                    );
            }
            return _assemblyBuilder;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
        }
    }
}
