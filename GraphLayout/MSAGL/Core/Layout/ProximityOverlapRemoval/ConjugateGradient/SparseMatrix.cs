/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;

namespace Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient {
    // 
    /// <summary>
    /// Matrix in compressed sparse row format (CSR) 
    /// </summary>
    public class SparseMatrix {
        /// <summary>
        /// Non-Zero matrix values from left-to-right, top-to-bottom
        /// </summary>
        private double[] values;
        /// <summary>
        /// Column index of the x-th value in matrix (left-to-right, top-to-bottom)
        /// </summary>
        private int[] col_ind;
        /// <summary>
        /// Row pointers, marking the start and end positions in values of the elements of a given row.
        /// </summary>
        private int[] row_ptr; //row_ptr.Length-1 is number of rows

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double[] Values(){return values;}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int[] ColInd() {return col_ind;}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int[] RowPtr() {return row_ptr;}


        private int numRow;
        /// <summary>
        /// Number of rows of the matrix.
        /// </summary>
        public int NumRow {
            set { numRow = value; }
            get { return numRow;} }
        /// <summary>
        /// Number of columns of the matrix.
        /// </summary>
        public int NumCol { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="valuesFlat">non zero values of the matrix, left-to-right and top-to-bottom</param>
        /// <param name="columnIndices">column indices of the the values</param>
        /// <param name="rowPointers">pointer to starting (and end) index of a row in values</param>
        /// <param name="numberColumns">number of columns of the matrix</param>
        public SparseMatrix(double[] valuesFlat, int[] columnIndices, int[] rowPointers, int numberColumns) {
            values = valuesFlat;
            col_ind = columnIndices;
            row_ptr = rowPointers;
            NumCol = numberColumns;
        }

       /// <summary>
        /// Creates and initializes data structures for the given size.
       /// </summary>
       /// <param name="numValues"></param>
       /// <param name="numRow"></param>
       /// <param name="numCol"></param>
        public SparseMatrix(int numValues, int numRow, int numCol) {
            //create sparse matrix data for weighted Laplacian Lw
            values = new double[numValues];
            col_ind = new int[numValues];
            row_ptr = new int[numRow+1];

            NumRow = numRow;
            NumCol = numCol;
        }

        /// <summary>
        /// Multiplies the given vector with the sparse matrix (only if the vector length corresponds to the number of columns in the matrix).
        /// </summary>
        /// <param name="m">matrix</param>
        /// <param name="vec">vector</param>
        /// <returns>vector with same amount of entries as the matrix has rows</returns>
        public static Vector operator *(SparseMatrix m,Vector vec) {
            if (vec.array.Length < m.NumCol) throw new ArgumentException("vector must have as many entries as the matrix has columns");
            double[] result=new double[m.NumRow];
            for (int row = 0; row < m.NumRow; row++) {//multiply vector with every row
                int startPos = m.row_ptr[row];
                int endPos = m.row_ptr[row + 1];
                for (int i = startPos; i < endPos; i++) {
                    int columnIndex = m.col_ind[i];
                    result[row] += vec.array[columnIndex]*m.values[i];
                }
            }
            return new Vector(result);
        }

        /// <summary>
        ///  Multiplies the given vector with the sparse matrix (only if the vector length corresponds to the number of columns in the matrix).
        /// </summary>
        /// <param name="m"></param>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static double[] operator *(SparseMatrix m, double[] vec) {
            return (m*new Vector(vec)).array;
        }

        /// <summary>
        /// Returns the inverted diagonal of this matrix.
        /// </summary>
        /// <returns></returns>
        public Vector DiagonalPreconditioner() {
            double[] result = new double[NumRow];
            for (int row = 0; row < NumRow; row++) {
                int startPos = row_ptr[row];
                int endPos = row_ptr[row + 1];
                for (int i = startPos; i < endPos; i++) {
                    int columnIndex = col_ind[i];
                    if (row == columnIndex && values[i] != 0) {//positive value on diagonal of matrix
                        result[row] = 1/values[i];    
                    }
                    
                }
            }
            return new Vector(result);
        }

        /// <summary>
        /// Returns the full blown up matrix. Attention: needs O(n^2) memory and time. Should only be used for debugging purposes.
        /// </summary>
        /// <returns></returns>
        public double[,] GetFullMatrix() {
#if SHARPKIT //https://code.google.com/p/sharpkit/issues/detail?id=340
            // SharpKit/Colin: multidimensional arrays not supported in JavaScript
            throw new NotSupportedException("Multi-dimensional arrays are not supported");
#else
            double[,] result = new double[NumRow,NumCol];
            for (int row = 0; row < NumRow; row++) {
                int startPos = row_ptr[row];
                int endPos = row_ptr[row + 1];
                for (int i = startPos; i < endPos; i++) {
                    int col = col_ind[i];
                    result[row,col] += values[i];
                }
            }
            return result;
#endif
        }

        /// <summary>
        /// Prints the full matrix.
        /// </summary>
        public void PrintMatrix() {
            var matrix = GetFullMatrix();
            int rowLength = matrix.GetLength(0);
            int colLength = matrix.GetLength(1);
            
            for (int i = 0; i < rowLength; i++) {
                for (int j = 0; j < colLength; j++) {
                    Console.Write(string.Format("{0} \t",matrix[i,j].ToString("0.00")));
                }
                Console.Write(Environment.NewLine+Environment.NewLine);
            }
        }
    }


}
