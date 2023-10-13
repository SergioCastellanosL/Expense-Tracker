﻿using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense_Tracker.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage ="Please select an account.")]
        public int AccountId { get; set; }
        public Account? Account { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        [Required(ErrorMessage ="Required")]
        public decimal Amount { get; set; }
        [Column(TypeName = "nvarchar(75)")]
        public string? Note { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        [NotMapped] 
        public string? CategoryTitleWithIcon { 
            get 
            { 
                return Category == null ? "" : Category.Icon+" "+ Category.Title;
            } 
        }
        [NotMapped]
        public string? FormattedAmount
        {
            get
            {
                return ((Category == null || Category.Type == "Expense")? "- " : "+ ") + Amount.ToString("C2");
            }
        }
    }
}
