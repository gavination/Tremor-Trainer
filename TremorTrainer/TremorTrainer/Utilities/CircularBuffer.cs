using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics;


namespace TremorTrainer.Utilities
{
    public class CircularBuffer
    {
        Complex32[] buffer;

        int head;
        int tail;
        bool full;

        public CircularBuffer(int size)
        {
            buffer = new Complex32[size];
            head = 0;
            tail = 0;
            full = false;
        }

        public void Push(Complex32 element)
        {
            buffer[head] = element;

            if(full)
            {
                tail = (tail + 1) % Capacity;
            }

            head = (head + 1) % Capacity;

            full = head == tail;
        }

        public int Count
        {
            get
            {
                if(!full)
                {
                    if(head >= tail)
                    {
                        return head - tail;
                    }
                    else
                    {
                        return Capacity + head - tail;
                    }
                }

                return Capacity;
            }
        }

        public int Capacity
        {
            get
            {
                return buffer.Length;
            }
        }

        public void Clear()
        {
            head = tail;   
        }

        // Due Credit: LordTocs: https://github.com/LordTocs
        public Complex32[] ToArray()
        {
            Complex32[] result = new Complex32[Count];
            for (int i = 0; i < Count; ++i)
            {
                result[i] = this[i];
            }
            return result;
        }

        // Due Credit: LordTocs: https://github.com/LordTocs
        public Complex32 this[int index]
        {
            get 
            {
                return buffer[(index + tail) % buffer.Length];
            }
            set 
            {
                buffer[(index + tail) % buffer.Length] = value;
            }
        }
    }
}
