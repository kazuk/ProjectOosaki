using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oosaki.Msil
{
    public class ModuleMetadataResolver : IMetadataTokenResolver
    {
        private readonly Module _module;
        private readonly Dictionary<Type,Type> _typeMap = new Dictionary<Type, Type>();
        private readonly Dictionary<MethodBase, MethodBase> _methodMap = new Dictionary<MethodBase, MethodBase>();

        public ModuleMetadataResolver(Module module)
        {
            _module = module;
        }

        public MethodBase ResolveMethod(int methodToken)
        {
            var resolveMethod = _module.ResolveMethod(methodToken);
            MethodBase mappedMethod;
            if (_methodMap.TryGetValue( resolveMethod, out mappedMethod)) return mappedMethod;
            return resolveMethod;
        }

        public string ResolveString(int token)
        {
            return _module.ResolveString(token);
        }

        public Type ResolveType(int token)
        {
            var resolveType = _module.ResolveType(token);
            Type mappedType;
            if (_typeMap.TryGetValue(resolveType, out mappedType)) return mappedType;
            return resolveType;
        }

        public void MapType(Type fromType, Type toType)
        {
            _typeMap.Add(fromType, toType);
        }

        public void MapMethod(MethodBase fromMethod, MethodBase toMethod)
        {
            _methodMap.Add(fromMethod, toMethod);
        }

        public FieldInfo ResolveField(int token)
        {
            return _module.ResolveField(token);
        }

        public Type ResolveTokenAsType(int token)
        {
            return _module.ResolveType(token);
        }

        public byte[] ResolveSignature(int tokenValue)
        {
            return _module.ResolveSignature(tokenValue);
        }
    }
}