namespace Sample
{
    public class Wrap
    {
        public int Total(int first, int second, int third, int fourth)
        {
            var total = Add(first, second) + Add(third, fourth) + Add(first, third) + Add(second, fourth) + Add(1, 2) + Add(3, 4) + Add(5, 6);
            return total;
        }

        public void Log(string first, string second)
        {
            Write(first, second, first, second, first, second, first, second, first, second, first, second, first, second, first, second);
            Write(second, first, second, first, second);
        }

        private static int Add(int left, int right) => left + right;

        private static void Write(params string[] values) => _ = values;
    }
}
