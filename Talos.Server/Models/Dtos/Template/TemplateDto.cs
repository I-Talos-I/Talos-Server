namespace Talos.Server.Models.Dtos;

public class TemplateDto
{
    public int Id { get; set; }
    public string Template_Name { get; set; }
    public string Slug { get; set; }
    public bool Is_Public { get; set; }
    public string License_Type { get; set; }
    public DateTime Create_At { get; set; }

    public int User_Id { get; set; }
}
