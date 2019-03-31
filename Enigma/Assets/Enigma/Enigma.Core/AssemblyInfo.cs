using System.Runtime.CompilerServices;

#if UNITY_EDITOR || DEBUG
    [assembly:InternalsVisibleTo("Tests")]
#endif
