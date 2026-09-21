using Microsoft.EntityFrameworkCore;
using People.DataLayer.Interface;
using People.DataLayer.Models;
using People.SharedLayer;

namespace People.DataLayer.Repository;

public class PeopleRepository : IPeopleRepository
{
    private readonly TrainingContext _context;

    public PeopleRepository(TrainingContext context)
    {
        _context = context;
    }

    // CREATE -----------------------------------------------------------
    public async Task<PersonResponseDto> AddPersonAsync(PersonRequestDto request)
    {
        Console.WriteLine("DataLayer -> Received PersonRequestDto (Add)");
        Console.WriteLine($"DataLayer -> Name: {request.PFName} {request.PSName}, Age: {request.PAge}");

        var newPerson = new PersonType
        {
            PFName = request.PFName,
            PSName = request.PSName,
            PAge = request.PAge,
            SslcMarks = request.SslcMarks,
            PucMarks = request.PucMarks,
            BeMarks = request.BeMarks,
            MeMarks = request.MeMarks,
            Hobby1 = request.Hobby1,
            Hobby2 = request.Hobby2,
            Hobby3 = request.Hobby3
        };

        Console.WriteLine("DataLayer -> Before database work (AddPeople stored procedure)");

        await _context.Procedures.AddPeopleAsync(new List<PersonType> { newPerson });

        Console.WriteLine("DataLayer -> After database work (AddPeople completed)");

        // AddPeople does not hand back the new PNo, so read the people back
        // and take the highest PNo as the one that was just added.
        var people = await GetAllPeopleAsync();
        var response = people.OrderByDescending(p => p.PNo).First();

        Console.WriteLine("DataLayer -> Returning PersonResponseDto (Add)");
        Console.WriteLine($"DataLayer -> PNo: {response.PNo}, TotalMarks: {response.TotalMarks}");

        return response;
    }

    // READ ---------------------------------------------------------------
    public async Task<List<PersonResponseDto>> GetAllPeopleAsync()
    {
        Console.WriteLine("DataLayer -> Before database work (GetPeople stored procedure)");

        var people = await _context.Procedures.GetPeopleAsync();

        var totalMarks = await _context.Peoples
            .Select(p => new { p.PNo, Total = TrainingContext.GetTotalMarks(p.PNo) })
            .ToListAsync();

        Console.WriteLine("DataLayer -> After database work (GetPeople completed)");

        var result = new List<PersonResponseDto>();

        foreach (var person in people)
        {
            var total = totalMarks.SingleOrDefault(t => t.PNo == person.PNo)?.Total ?? 0;

            result.Add(new PersonResponseDto
            {
                PNo = person.PNo,
                PFName = person.PFName,
                PSName = person.PSName,
                PAge = person.PAge,
                SslcMarks = person.SslcMarks ?? 0,
                PucMarks = person.PucMarks ?? 0,
                BeMarks = person.BeMarks,
                MeMarks = person.MeMarks,
                Hobby1 = person.Hobby1,
                Hobby2 = person.Hobby2,
                Hobby3 = person.Hobby3,
                TotalMarks = total
            });
        }

        Console.WriteLine($"DataLayer -> Returning {result.Count} PersonResponseDto object(s) (Get)");

        return result;
    }

    // UPDATE ---------------------------------------------------------------
    public async Task<PersonResponseDto> UpdatePersonAsync(PersonRequestDto request)
    {
        if (request.PNo is null)
        {
            throw new ArgumentException("PNo is required to update a person.");
        }

        Console.WriteLine("DataLayer -> Received PersonRequestDto (Update)");
        Console.WriteLine($"DataLayer -> PNo: {request.PNo}, Name: {request.PFName} {request.PSName}");

        Console.WriteLine("DataLayer -> Before database work (UpdatePerson stored procedure)");

        await _context.Procedures.UpdatePersonAsync(
            request.PNo,
            request.PFName,
            request.PSName,
            request.PAge,
            request.SslcMarks,
            request.PucMarks,
            request.BeMarks,
            request.MeMarks,
            request.Hobby1,
            request.Hobby2,
            request.Hobby3);

        Console.WriteLine("DataLayer -> After database work (UpdatePerson completed)");

        var response = new PersonResponseDto
        {
            PNo = request.PNo.Value,
            PFName = request.PFName,
            PSName = request.PSName,
            PAge = request.PAge,
            SslcMarks = request.SslcMarks,
            PucMarks = request.PucMarks,
            BeMarks = request.BeMarks,
            MeMarks = request.MeMarks,
            Hobby1 = request.Hobby1,
            Hobby2 = request.Hobby2,
            Hobby3 = request.Hobby3,
            TotalMarks = request.SslcMarks + request.PucMarks + (request.BeMarks ?? 0) + (request.MeMarks ?? 0)
        };

        Console.WriteLine("DataLayer -> Returning PersonResponseDto (Update)");
        Console.WriteLine($"DataLayer -> PNo: {response.PNo}, TotalMarks: {response.TotalMarks}");

        return response;
    }

    // DELETE ---------------------------------------------------------------
    public async Task<PersonResponseDto> DeletePersonAsync(PersonRequestDto request)
    {
        if (request.PNo is null)
        {
            throw new ArgumentException("PNo is required to delete a person.");
        }

        var pNo = request.PNo.Value;

        Console.WriteLine("DataLayer -> Received PersonRequestDto (Delete)");
        Console.WriteLine($"DataLayer -> PNo: {pNo}");

        // Read the record before deleting it, so we have something to return.
        var people = await GetAllPeopleAsync();
        var response = people.FirstOrDefault(p => p.PNo == pNo) ?? new PersonResponseDto { PNo = pNo };

        Console.WriteLine("DataLayer -> Before database work (ExecuteDeleteAsync)");

        // Delete child tables first, then the parent table, using ExecuteDeleteAsync.
        await _context.PeopleHobbies.Where(h => h.PNo == pNo).ExecuteDeleteAsync();
        await _context.PeopleMarks.Where(m => m.PNo == pNo).ExecuteDeleteAsync();
        await _context.Peoples.Where(p => p.PNo == pNo).ExecuteDeleteAsync();

        Console.WriteLine("DataLayer -> After database work (ExecuteDeleteAsync completed)");
        Console.WriteLine("DataLayer -> Returning PersonResponseDto (Delete)");
        Console.WriteLine($"DataLayer -> PNo: {response.PNo}");

        return response;
    }

    // F1: Peoples JOIN PeopleMarks JOIN PeopleHobbies (learning function) -----
    public async Task<List<PersonResponseDto>> GetPeopleUsingFirstJoinAsync()
    {
        Console.WriteLine("DataLayer -> F1: Peoples JOIN PeopleMarks JOIN PeopleHobbies (before database work)");

        var query =
            from person in _context.Peoples
            join mark in _context.PeopleMarks
                on person.PNo equals mark.PNo
            join hobby in _context.PeopleHobbies
                on mark.PNo equals hobby.PNo
            select new PersonResponseDto
            {
                PNo = person.PNo,
                PFName = person.PFName,
                PSName = person.PSName,
                PAge = person.PAge,
                SslcMarks = mark.SslcMarks,
                PucMarks = mark.PucMarks,
                BeMarks = mark.BeMarks,
                MeMarks = mark.MeMarks,
                Hobby1 = hobby.Hobby1,
                Hobby2 = hobby.Hobby2,
                Hobby3 = hobby.Hobby3,
                TotalMarks = mark.SslcMarks + mark.PucMarks + (mark.BeMarks ?? 0) + (mark.MeMarks ?? 0)
            };

        var result = await query.ToListAsync();

        Console.WriteLine($"DataLayer -> F1: after database work, {result.Count} PersonResponseDto object(s) returned");

        return result;
    }

    // F2: Peoples JOIN PeopleMarks, and Peoples JOIN PeopleHobbies (learning function) -----
    public async Task<List<PersonResponseDto>> GetPeopleUsingSecondJoinAsync()
    {
        Console.WriteLine("DataLayer -> F2: Peoples JOIN PeopleMarks, Peoples JOIN PeopleHobbies (before database work)");

        var query =
            from person in _context.Peoples
            join mark in _context.PeopleMarks
                on person.PNo equals mark.PNo
            join hobby in _context.PeopleHobbies
                on person.PNo equals hobby.PNo
            select new PersonResponseDto
            {
                PNo = person.PNo,
                PFName = person.PFName,
                PSName = person.PSName,
                PAge = person.PAge,
                SslcMarks = mark.SslcMarks,
                PucMarks = mark.PucMarks,
                BeMarks = mark.BeMarks,
                MeMarks = mark.MeMarks,
                Hobby1 = hobby.Hobby1,
                Hobby2 = hobby.Hobby2,
                Hobby3 = hobby.Hobby3,
                TotalMarks = mark.SslcMarks + mark.PucMarks + (mark.BeMarks ?? 0) + (mark.MeMarks ?? 0)
            };

        var result = await query.ToListAsync();

        Console.WriteLine($"DataLayer -> F2: after database work, {result.Count} PersonResponseDto object(s) returned");

        return result;
    }
}
