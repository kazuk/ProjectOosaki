namespace Oosaki.Msil
{
    public enum NonIlOffsetTag :byte
    {
        BeginTryBlock = 0x80,
        EndTryBlock = 0x81,
        BeginCatchBlock = 0x82,
        EndCatchBlock = 0x83,
        BeginFinallyBlock = 0x84,
        EndFinallyBlock = 0x85,
        BeginFilterBlock = 0x86,
        Label = 0x87
    }
}