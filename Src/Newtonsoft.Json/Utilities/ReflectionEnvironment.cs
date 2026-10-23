#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Runtime.CompilerServices;
using System.Security;
#if HAVE_CAS
using System.Security.Permissions;
#endif

namespace Newtonsoft.Json.Utilities
{
    /// <summary>
    /// Capabilities of the current runtime that decide how reflection is performed:
    /// whether dynamic code can be generated, whether the code is fully trusted,
    /// and which <see cref="ReflectionDelegateFactory"/> to use.
    /// </summary>
    internal static class ReflectionEnvironment
    {
        private static bool? _dynamicCodeGeneration;
        private static bool? _fullyTrusted;

#if DEBUG
        internal static void SetFullyTrusted(bool? fullyTrusted)
        {
            _fullyTrusted = fullyTrusted;
        }

        internal static void SetDynamicCodeGeneration(bool dynamicCodeGeneration)
        {
            _dynamicCodeGeneration = dynamicCodeGeneration;
        }
#endif

        public static bool DynamicCodeGeneration
        {
#if HAVE_SECURITY_SAFE_CRITICAL_ATTRIBUTE
            [SecuritySafeCritical]
#endif
            get
            {
                if (_dynamicCodeGeneration == null)
                {
#if HAVE_DYNAMIC_CODE_COMPILED
                    _dynamicCodeGeneration = RuntimeFeature.IsDynamicCodeCompiled;
#elif HAVE_CAS
                    try
                    {
                        new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
                        new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess).Demand();
                        new SecurityPermission(SecurityPermissionFlag.SkipVerification).Demand();
                        new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                        new SecurityPermission(PermissionState.Unrestricted).Demand();
                        _dynamicCodeGeneration = true;
                    }
                    catch (Exception)
                    {
                        _dynamicCodeGeneration = false;
                    }
#else
                    _dynamicCodeGeneration = false;
#endif
                }

                return _dynamicCodeGeneration.GetValueOrDefault();
            }
        }

        public static bool FullyTrusted
        {
            get
            {
                if (_fullyTrusted == null)
                {
#if (DOTNET || PORTABLE || PORTABLE40)
                    _fullyTrusted = true;
#elif !(NET20 || NET35 || PORTABLE40)
                    AppDomain appDomain = AppDomain.CurrentDomain;

                    _fullyTrusted = appDomain.IsHomogenous && appDomain.IsFullyTrusted;
#else
                    try
                    {
                        new SecurityPermission(PermissionState.Unrestricted).Demand();
                        _fullyTrusted = true;
                    }
                    catch (Exception)
                    {
                        _fullyTrusted = false;
                    }
#endif
                }

                return _fullyTrusted.GetValueOrDefault();
            }
        }

        public static ReflectionDelegateFactory ReflectionDelegateFactory
        {
            get
            {
#if !(PORTABLE40 || PORTABLE || DOTNET || NETSTANDARD2_0)
                if (DynamicCodeGeneration)
                {
                    return DynamicReflectionDelegateFactory.Instance;
                }

                return LateBoundReflectionDelegateFactory.Instance;
#else
                return ExpressionReflectionDelegateFactory.Instance;
#endif
            }
        }
    }
}
