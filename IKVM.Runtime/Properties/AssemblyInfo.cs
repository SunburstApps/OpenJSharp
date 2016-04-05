using System.Security;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("IKVM.Runtime")]
[assembly: InternalsVisibleTo("IKVM.Runtime.JNI")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Core")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Util")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Security")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Management")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Media")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Misc")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.Remoting")]
[assembly: InternalsVisibleTo("IKVM.OpenJDK.SwingAWT")]

[assembly: AllowPartiallyTrustedCallers]
