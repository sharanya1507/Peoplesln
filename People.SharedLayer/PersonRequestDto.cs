#nullable disable
namespace People.SharedLayer;

public class PersonRequestDto
{
    // Set only for Update/Delete. Left null for Add (the database assigns it).
    public int? PNo { get; set; }

    public string PFName { get; set; }

    public string PSName { get; set; }

    public int PAge { get; set; }

    public int SslcMarks { get; set; }

    public int PucMarks { get; set; }

    public int? BeMarks { get; set; }

    public int? MeMarks { get; set; }

    public string Hobby1 { get; set; }

    public string Hobby2 { get; set; }

    public string Hobby3 { get; set; }
}
