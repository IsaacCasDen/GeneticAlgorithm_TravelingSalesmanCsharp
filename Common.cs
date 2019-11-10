
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common {
    public static class Common {
        internal static void Deconstruct<T>(this List<T> list, out T head, out List<T> tail) {
            head = list.FirstOrDefault();
            tail = new List<T>(list.Skip(1));
        }
        internal static void Deconstruct<T>(this T[] list, out T head, out T[] tail) {
            head = list.FirstOrDefault();
            tail = list.Skip(1).ToArray();
        }
        public static decimal fromSigFigs(decimal value, int sigFigs) {
            decimal _value = value;
            int places = 0;

            if (value==0) return _value;

            while (Math.Abs((int)_value)<=0) {
                _value*=10;
                places++;
            }

            _value = Math.Round(_value,sigFigs-1);
            if (places>0) { _value = _value/(decimal)Math.Pow(10,places); }

            return _value;
        }
        //https://stackoverflow.com/questions/4124189/performing-math-operations-on-decimal-datatype-in-c
        public static decimal Sqrt(decimal x, decimal? guess = null)
        {
            if (x==0) return 0;
            var ourGuess = guess.GetValueOrDefault(x / 2m);
            var result = x / ourGuess;
            var average = (ourGuess + result) / 2m;

            if (average == ourGuess) // This checks for the maximum precision possible with a decimal.
                return average;
            else
                return Sqrt(x, average);
        }
        public static decimal Pow(decimal x, int pow)
        {
            if (pow==0) return 1;
            
            decimal value =x;
            
            switch(pow>0) {
                case true:
                    for (int i=0; i<pow-1; i++) value*=value;
                    break;
                case false:
                    for (int i=0; i>pow+1; i--) value*=value;
                    value=1/value;
                    break;
            }

            return value;
        }
        internal static T[] Slice<T>(this T[] array, int position) {
            if (array.Length<=1) {
                return new T[0];
            } 

            T[] newArray;
            
            if (position<0) {
                newArray = new T[array.Length];
                Array.Copy(array,newArray,array.Length);
            } else if (position>=array.Length) {
                newArray = new T[array.Length];
                Array.Copy(array,newArray,array.Length);
            } else {
                newArray = new T[array.Length-1];
                if (position==0) {
                    Array.Copy(array.Skip(1).ToArray(),newArray,newArray.Length);
                } else if (position==array.Length-1) {
                    Array.Copy(array.Take(newArray.Length).ToArray(),newArray,newArray.Length);
                } else {
                    int i=0;
                    for (int p=0; p<array.Length; p++) {
                        if (p==position) continue;
                        newArray[i++]=array[p];
                    }
                }
            } 

            return newArray;
        }       
    }
    public class LabelCounts {
        public int Positive {get;set;} = 0;
        public int Negative {get;set;} = 0;

        public LabelCounts clone() {
            return new LabelCounts() {Positive=this.Positive, Negative=this.Negative};
        }
    }
}
