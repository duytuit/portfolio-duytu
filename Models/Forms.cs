using System.ComponentModel.DataAnnotations;
namespace PortfolioMvc.Models;
public class ContactForm {
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(200)] public string Subject { get; set; } = "";
    [Required, StringLength(5000)] public string Message { get; set; } = "";
    public string ReturnPath { get; set; } = "/";
}
public class NewsletterForm {
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    public string ReturnPath { get; set; } = "/";
}
