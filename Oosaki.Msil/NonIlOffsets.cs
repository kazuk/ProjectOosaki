using System.Diagnostics.Contracts;

namespace Oosaki.Msil
{
    public static class NonIlOffsets
    {

        public static int BeginTryBlock(int exceptionHandlingIndex)
        {
            return (((int)NonIlOffsetTag.BeginTryBlock) << 24) | exceptionHandlingIndex;
        }

        public static int EndTryBlock(int exceptionHandlingIndex)
        {
            return (((int)NonIlOffsetTag.EndTryBlock) << 24) | exceptionHandlingIndex;
        }

        public static int BeginCatchBlock(int exceptionHandlingIndex)
        {
            return (((int)NonIlOffsetTag.BeginCatchBlock) << 24) | exceptionHandlingIndex;
        }

        public static int BeginFinallyBlock(int exceptionHandlingIndex)
        {
            return (((int)NonIlOffsetTag.BeginFinallyBlock) << 24) | exceptionHandlingIndex;
        }

        internal static int EndHandlerBlock(int exceptionHandlingIndex)
        {
            return (((int)NonIlOffsetTag.EndCatchBlock) << 24) | exceptionHandlingIndex;
        }

        public static int BeginFilterBlock(int exceptionHandlingIndex)
        {
            return (((int)NonIlOffsetTag.BeginFilterBlock) << 24) | exceptionHandlingIndex;
        }

        public static bool IsTryBlockBegin(int ilOffset, out int handlingClauseIndex)
        {
            Contract.Ensures(
                Contract.Result<bool>()
                    ? Contract.ValueAtReturn(out handlingClauseIndex) >= 0
                    : Contract.ValueAtReturn(out handlingClauseIndex) == -1);

            handlingClauseIndex = ilOffset & 0xffffff;
            if (((ilOffset >> 24) & 0xff) == (int) NonIlOffsetTag.BeginTryBlock)
            {
                return true;
            }
            handlingClauseIndex = -1;
            return false;
        }

        public static bool IsTryBlockEnd(int ilOffset, out int handlingClauseIndex)
        {
            Contract.Ensures(
                Contract.Result<bool>()
                    ? Contract.ValueAtReturn(out handlingClauseIndex) >= 0
                    : Contract.ValueAtReturn(out handlingClauseIndex) == -1);

            handlingClauseIndex = ilOffset & 0xffffff;
            if (((ilOffset >> 24) & 0xff) == (int)NonIlOffsetTag.EndTryBlock)
            {
                return true;
            }
            handlingClauseIndex = -1;
            return false;
        }

        public static bool IsCatchBlockBegin(int ilOffset, out int handlingClauseIndex)
        {
            Contract.Ensures(
                Contract.Result<bool>()
                    ? Contract.ValueAtReturn(out handlingClauseIndex) >= 0
                    : Contract.ValueAtReturn(out handlingClauseIndex) == -1);

            handlingClauseIndex = ilOffset & 0xffffff;
            if (((ilOffset >> 24) & 0xff) == (int)NonIlOffsetTag.BeginCatchBlock)
            {
                return true;
            }
            handlingClauseIndex = -1;
            return false;
        }

        internal static bool IsCatchBlockEnd(int ilOffset, out int handlingClauseIndex)
        {
            Contract.Ensures(
                Contract.Result<bool>()
                    ? Contract.ValueAtReturn(out handlingClauseIndex) >= 0
                    : Contract.ValueAtReturn(out handlingClauseIndex) == -1);

            handlingClauseIndex = ilOffset & 0xffffff;
            if (((ilOffset >> 24) & 0xff) == (int)NonIlOffsetTag.EndCatchBlock)
            {
                return true;
            }
            handlingClauseIndex = -1;
            return false;
        }

        public static bool IsFinallyBlockBegin(int ilOffset, out int handlingClauseIndex)
        {
            Contract.Ensures(
                Contract.Result<bool>()
                    ? Contract.ValueAtReturn(out handlingClauseIndex) >= 0
                    : Contract.ValueAtReturn(out handlingClauseIndex) == -1);

            handlingClauseIndex = ilOffset & 0xffffff;
            if (((ilOffset >> 24) & 0xff) == (int)NonIlOffsetTag.BeginFinallyBlock)
            {
                return true;
            }
            handlingClauseIndex = -1;
            return false;
        }
        public static bool IsFinallyBlockEnd(int ilOffset, out int handlingClauseIndex)
        {
            Contract.Ensures(
                Contract.Result<bool>()
                    ? Contract.ValueAtReturn(out handlingClauseIndex) >= 0
                    : Contract.ValueAtReturn(out handlingClauseIndex) == -1);

            handlingClauseIndex = ilOffset & 0xffffff;
            if (((ilOffset >> 24) & 0xff) == (int)NonIlOffsetTag.EndFinallyBlock)
            {
                return true;
            }
            handlingClauseIndex = -1;
            return false;
        }

        public static int Label( int id )
        {
            return (((int) NonIlOffsetTag.Label) << 24) | id;
        }

        public static bool IsLabel(int ilOffset,out int id )
        {
            if (((ilOffset >> 24) & 0xff) == (int)NonIlOffsetTag.Label)
            {
                id = ilOffset & 0xffffff;
                return true;
            }
            id = 0;
            return false;
        }




    }
}