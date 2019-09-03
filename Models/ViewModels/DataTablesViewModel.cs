using System;
using System.Collections.Generic;

namespace DataTables.Models
{
    public class DataTablesViewModel
    {
        public DataTablesViewModel(int draw, int recordsTotal, int recordsFiltered, IReadOnlyCollection<object[]> data)
        {
            if (draw <= 0)
            {
                throw new ArgumentException("Draw cannot be less or equal than 0");
            }
            if (recordsFiltered > recordsTotal)
            {
                throw new ArgumentException("Filtered records cannot be greater than total records");
            }
            if (recordsFiltered < 0)
            {
                throw new ArgumentException("Filtered records cannot be less than 0");
            }
            if (recordsTotal < 0)
            {
                throw new ArgumentException("Total records cannot be less than 0");
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            Draw = draw;
            RecordsTotal = recordsTotal;
            RecordsFiltered = recordsFiltered;
            Data = data;
        }
        public int Draw { get; }
        public int RecordsTotal { get; }
        public int RecordsFiltered { get; }
        public IReadOnlyCollection<object[]> Data { get; }
    }
}