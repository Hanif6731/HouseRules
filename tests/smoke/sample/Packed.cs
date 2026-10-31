namespace Sample
{
    internal static class Packed
    {
        public static int Total() => Sum(
            1, 2, 3);

        public static int Sum(
            int first, int second, int third) => first + second + third;

        public static int Next() => Apply(
            1, value => value + 1);

        private static int Apply(int start, Func<int, int> step) => step(start);
    }
}
