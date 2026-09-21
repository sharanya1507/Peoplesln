using People.SharedLayer;

namespace People_WinUI;

public class PersonRow
{
    public PersonResponseDto Dto { get; }

    public PersonRow(PersonResponseDto dto)
    {
        Dto = dto;
    }

    public int PNo => Dto.PNo;

    public string PFName => Dto.PFName;

    public string PSName => Dto.PSName;

    public int PAge => Dto.PAge;

    public int SslcMarks => Dto.SslcMarks;

    public int PucMarks => Dto.PucMarks;

    public string BeMarksText => Dto.BeMarks?.ToString() ?? string.Empty;

    public string MeMarksText => Dto.MeMarks?.ToString() ?? string.Empty;

    public string Hobby1 => Dto.Hobby1;

    public string Hobby2 => Dto.Hobby2;

    public string Hobby3 => Dto.Hobby3;

    public int TotalMarks => Dto.TotalMarks;
}
