namespace Assets.Scripts
{
    public static class Ticker
    {
        public static bool Should(float elapsed, ref float runningTotal, float target)
        {
            runningTotal += elapsed;
            if(runningTotal >= target)
            {
                runningTotal -= target;
                return true;
            }

            return false;
        }
    }
}
