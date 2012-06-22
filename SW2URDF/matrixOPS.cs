﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Reflection;

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using MathNet.Numerics.LinearAlgebra.Generic;
using MathNet.Numerics.LinearAlgebra.Double;

namespace MatrixOPS
{
    public class ops
    {
        private int firstFreeRow;
        public Matrix str2mat(string S)
        {
            S = S.Trim(new char[] { '[', ']', ' ' });          
            string[] rows = S.Split(';');
            int rowCount = rows.Length;
            string[] firstRow = rows[0].Split(' ');
            int columnCount = firstRow.Length;
            Matrix m = new DenseMatrix(rowCount, columnCount);
            for (int i = 0; i < rowCount; i++)
            {
                rows[i] = rows[i].Trim();
                string[] columns = rows[i].Split(' ');
                if (columns.Length == columnCount)
                {
                    double value;
                    for (int j = 0; j < columnCount; j++)
                    {

                        m[i, j] = (Double.TryParse(columns[j], out value)) ? value : 0;
                    }
                }
            }
            return m;
        }
        public string mat2str(Matrix m)
        {
            string s = "[";
            for (int i = 0; i < m.RowCount; i++)
            {
                for (int j = 0; j < m.ColumnCount; j++)
                {
                    s = s.Insert(s.Length, m[i, j].ToString());
                    if (j != m.ColumnCount - 1)
                    {
                        s = s.Insert(s.Length, " ");
                    }
                }
                if (i != m.RowCount - 1)
                {
                    s = s.Insert(s.Length, ";");
                }
            }
            s = s.Insert(s.Length, "]");
            return s;
        }
        public Vector vectorCat(Vector v1, Vector v2)
        {
            Vector vec = new DenseVector(v1.Count + v2.Count);
            v1.CopyTo(vec, 0, 0, v1.Count);
            v2.CopyTo(vec, 0, v1.Count, v2.Count);
            return vec;
        }
        public Matrix rref(Matrix m)
        {
            int minDimension = Math.Min(m.ColumnCount, m.RowCount);
            // Swap rows to get prepared for row echelon form
            for (int i = 0; i < minDimension; i++)
            {
                for (int j = i; j < minDimension; j++)
                {
                    Vector v = (Vector)m.Row(j);
                    if (v[i] != 0)
                    {
                        m.SetRow(j, m.Row(i));
                        m.SetRow(i, v);
                        break;
                    }
                }
            }
            for (int i = 0; i < m.RowCount; i++)
            {
                Vector v1 = (Vector)m.Row(i);
                int index = -1;

                //Find the first non-zero entry in row i (will either by the diagonal or to its right)
                for (int j = i; j < m.ColumnCount; j++)
                {
                    if (v1[j] != 0)
                    {
                        index = j;
                        break;
                    }
                }

                //If there are no more non-zero entries to be found, the matrix is now in row echelon form (and then some)
                if (index == -1)
                {
                    break;
                }
                for (int j = 0; j < m.RowCount; j++)
                {
                    if (i != j)
                    {

                        Vector v2 = (Vector)m.Row(j);
                        m.SetRow(j, v1 * v2[index] / v1[index] - v2);
                    }
                }
            }

            //Reduce the left most values to 1
            for (int i = 0; i < m.RowCount; i++)
            {
                Vector v = (Vector)m.Row(i);
                int index = -1;
                //Find the first non-zero entry in this row vector
                for (int j = i; j < m.ColumnCount; j++)
                {
                    if (v[j] != 0)
                    {
                        index = j;
                        break;
                    }
                }
                //If there are no more non-zero entries to be found, the matrix is now in reduced row echelon form
                if (index == -1)
                {
                    break;
                }
                m.SetRow(i, v / v[i]);
            }
            return m;
        }

        public Matrix nullSpace(Matrix m)
        {
            m = rref(m); //Null(A) = Null(rref(A))
            Matrix null_m = new DenseMatrix(m.ColumnCount, m.RowCount);

            int lead = 0;
            Vector zeros = new DenseVector(m.RowCount);
            Vector column = new DenseVector(m.RowCount);
            for (int i = 0; i < m.ColumnCount; i++)
            {
                if (m[i, lead] == 1)
                {
                    null_m.SetColumn(i, zeros);
                    lead++;
                }
                else
                {
                    for (int j = 0; j < lead; j++)
                    {
                        int columnIndex = findLeadingOneinVector((DenseVector)m.Row(j), 0, i-1);
                        column[columnIndex] = m[j, i];
                    }
                    null_m.SetColumn(i, column);
                }
            }

            return null_m;
        }

        // These sets of methods finds the bottom-most one in a column vector from a matrix.
        public int findLeadingOneinVector(Vector v)
        {
            return findLeadingOneinVector(v, 0, v.Count);
        }
        // Sets a lower bound in case this vector only has values thare are to the right of other leading ones
        public int findLeadingOneinVector(Vector v, int lowerBound)
        {
            return findLeadingOneinVector(v, lowerBound, v.Count);
        }
        // Sets an upper bound to reduce the number of computations. I.E in a rref matrix the 1 should be on or above the diagonal
        public int findLeadingOneinVector(Vector v, int lowerBound, int upperBound)
        {
            // If the upperBound is less than the lowerBound, the vector is searched backwards (to help speed up computation in some cases)
            if (upperBound < lowerBound)
            {
                for (int i = upperBound - 1; i >= lowerBound; i--)
                {
                    if (v[i] == 1)
                    {
                        return i;
                    }
                }
                return -1;
            }
            else
            {
                for (int i = lowerBound; i < upperBound; i++)
                {
                    if (v[i] == 1)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }



        public Matrix addConstraintVectorToMatrix(Matrix m, Vector v)
        {
            if (m.ColumnCount != v.Count)
            {
                return m;
            }
            int row = firstEmptyRow(m);
            if (row == -1)
            {
                return m;
            }
            m.SetRow(row, v);
            return m;
        }

        public bool isLinearlyIndependent(Matrix m, Vector v)
        {
            return true;
        }

        public int firstEmptyRow(Matrix m)
        {
            for (int i = 0; i < m.RowCount; i++)
            {
                bool isEmpty = true;
                for (int j = 0; j < m.ColumnCount; j++)
                {
                    if (m[i, j] != 0)
                    {
                        isEmpty = false;
                        break;
                    }
                }
                if (isEmpty)
                {
                    return i;
                }
                
            }
            return -1;
        }
    }
}