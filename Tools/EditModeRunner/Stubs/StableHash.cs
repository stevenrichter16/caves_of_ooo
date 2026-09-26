namespace CooRun
{
    public static class StableHash
    {
        // .NET Framework legacy (32-bit) string hash: deterministic across processes.
        public static int Of(string s)
        {
            if (s == null) return 0;
            unchecked
            {
                int hash1 = (5381 << 16) + 5381, hash2 = hash1;
                for (int i = 0; i < s.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ s[i];
                    if (i + 1 < s.Length) hash2 = ((hash2 << 5) + hash2) ^ s[i + 1];
                }
                return hash1 + hash2 * 1566083941;
            }
        }
    }
}
