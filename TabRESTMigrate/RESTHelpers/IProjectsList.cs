/// <summary>
/// Questions everything that manages a set of projects needs to be able to answer
/// </summary>
internal interface IProjectsList
{
    SiteProject FindProjectWithId(string projectId);
    SiteProject FindProjectWithName(string projectName);
}
