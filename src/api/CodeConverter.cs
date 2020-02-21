using System;

namespace CodeFlip.CodeJar.Api
{
    public class CodeConverter
    {
        readonly string _alphabet;
        public CodeConverter(string alphabet)
        {
            _alphabet = alphabet;
        }

        public string Alphabet
        {
            get { return _alphabet; }
        }

        public string ConvertToCode(int seedValue)
        {
            var encBase = _alphabet.Length;

            var digits = "";
            var num = seedValue;

            if (num == 0)
                return _alphabet[0].ToString();

            while (num > 0)
            {
                digits = _alphabet[num % encBase] + digits;
                num = num / encBase;
            }

            var result = digits;
            result = result.PadLeft(6, _alphabet[0]);

            return result;
        }

        public int ConvertFromCode(string code)
        {
            var result = 0;

            for (int i = 0; i < code.Length; i++)
            {
                var c = code[code.Length - 1 - i];
                var index = _alphabet.IndexOf(c);
                var p = index * (int)Math.Pow(_alphabet.Length, i);
                
                result = result + p;
            }

            return result;
        }
    }
}