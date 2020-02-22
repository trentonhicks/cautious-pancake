namespace CodeFlip.CodeJar.Api
{
    public static class States
    {
        public static byte Active = 0;
        public static byte Redeemed = 1;
        public static byte Inactive = 2;

        public static string ConvertToString(byte state)
        {
            var stateString = "";

            switch(state)
            {
                case 0:
                    stateString = "Active";
                    break;
                case 1:
                    stateString = "Redeemed";
                    break;
                case 2:
                    stateString = "Inactive";
                    break;
            }

            return stateString;
        }
        public static byte ConvertToByte(string state)
        {
            byte stateByte = 0;

            switch(state)
            {
                case "Active":
                    stateByte = 0;
                    break;
                case "Redeemed":
                    stateByte = 1;
                    break;
                case "Inactive":
                    stateByte = 2;
                    break;
            }

            return stateByte;
        }
    }
}