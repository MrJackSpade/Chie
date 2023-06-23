using System.Text;

namespace Ai.Utils
{
    public static class NewlineReplacer
    {
        public static string Replace(string inStr)
        {
            StringBuilder stringBuilder = new();

            bool lastR = false;
            bool lastE = false;
            foreach (char c in inStr)
            {
                switch (c)
                {
                    case '\r':
                        if (!lastE)
                        {
                            stringBuilder.Append('\\');
                            stringBuilder.Append(c);
                        }

                        lastR = true;
                        lastE = false;
                        break;

                    case '\n':
                        if (!lastR && !lastE)
                        {
                            stringBuilder.Append('\\');
                            stringBuilder.Append('\r');
                        }

                        stringBuilder.Append(c);
                        lastR = false;
                        lastE = false;
                        break;

                    case '/':
                    case '\\':
                        stringBuilder.Append(c);
                        lastE = true;
                        lastR = false;
                        break;

                    default:
                        stringBuilder.Append(c);
                        lastR = false;
                        lastE = false;
                        break;
                }
            }

            return stringBuilder.ToString();
        }
    }
}