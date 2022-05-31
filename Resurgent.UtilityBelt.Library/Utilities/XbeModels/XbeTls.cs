using System.Runtime.InteropServices;

namespace Resurgent.UtilityBelt.Library.Utilities.XbeModels
{ 
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XbeTls                        
    {
        public uint DataStartAddr;    
        public uint DataEndAddr;      
        public uint TlsIndexAddr;       
        public uint TlsCallbackAddr;      
        public uint SizeofZeroFill;    
        public uint Characteristics;     
    }
}
