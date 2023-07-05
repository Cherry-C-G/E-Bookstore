using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCartMvcUI.Models
{
    [Table("Genre")]
    public class Genre
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [MaxLength(40)]
        public string GenreName { get; set; }
        public List<Book> Books { get; set; }
    }
}