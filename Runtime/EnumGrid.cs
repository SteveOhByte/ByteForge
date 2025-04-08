using System;
using UnityEngine;

namespace ByteForge.Runtime
{
    [Serializable]
    public class EnumGrid<T1> where T1 : Enum
    {
        public int Rows => rows;
        public int Columns => columns;

        public T1[] Grid => grid;

        [SerializeField] private T1[] grid;

        private int rows;
        private int columns;

        public EnumGrid(int rows, int columns)
        {
            this.rows = rows;
            this.columns = columns;
            grid = new T1[rows * columns];
        }

        public T1 this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= rows || col < 0 || col >= columns)
                    throw new IndexOutOfRangeException();
                return grid[row * columns + col];
            }
            set
            {
                if (row < 0 || row >= rows || col < 0 || col >= columns)
                    throw new IndexOutOfRangeException();
                grid[row * columns + col] = value;
            }
        }
    }
}