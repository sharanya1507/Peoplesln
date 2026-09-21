using People.SharedLayer;

namespace People.DataLayer.Interface;

public interface IPeopleRepository
{
    // CREATE: Object in (PersonRequestDto) -> Object out (PersonResponseDto)
    Task<PersonResponseDto> AddPersonAsync(PersonRequestDto request);

    // READ: Object out (list of PersonResponseDto)
    Task<List<PersonResponseDto>> GetAllPeopleAsync();

    // UPDATE: Object in (PersonRequestDto, PNo set) -> Object out (PersonResponseDto)
    Task<PersonResponseDto> UpdatePersonAsync(PersonRequestDto request);

    // DELETE: Object in (PersonRequestDto, only PNo used) -> Object out (PersonResponseDto)
    Task<PersonResponseDto> DeletePersonAsync(PersonRequestDto request);

    // F1: Peoples JOIN PeopleMarks JOIN PeopleHobbies (learning function)
    Task<List<PersonResponseDto>> GetPeopleUsingFirstJoinAsync();

    // F2: Peoples JOIN PeopleMarks, and Peoples JOIN PeopleHobbies (learning function)
    Task<List<PersonResponseDto>> GetPeopleUsingSecondJoinAsync();
}
