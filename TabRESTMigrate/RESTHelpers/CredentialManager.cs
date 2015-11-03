using System;
using System.Collections.Generic;
using System.Text;

partial class CredentialManager
{

    List<CredentialNode> _workbookCredentials = new List<CredentialNode>();
    List<CredentialNode> _datasourceCredentials = new List<CredentialNode>();

    /// <summary>
    /// Looks through the set of credentials we are managing. If a match is found, its returned
    /// </summary>
    /// <param name="contentName"></param>
    /// <param name="projectName"></param>
    /// <returns></returns>
    public Credential FindWorkbookCredential(string workbookName, string projectName)
    {
        return FindContentCredential(_workbookCredentials, workbookName, projectName);
    }

    /// <summary>
    /// Looks through the set of credentials we are managing. If a match is found, its returned
    /// </summary>
    /// <param name="contentName"></param>
    /// <param name="projectName"></param>
    /// <returns></returns>
    public Credential FindDatasourceCredential(string datasourceName, string projectName)
    {
        return FindContentCredential(_datasourceCredentials, datasourceName, projectName);
    }

    /// <summary>
    /// Looks through the set of credentials we are managing. If a match is found, its returned
    /// </summary>
    /// <param name="list"></param>
    /// <param name="contentName"></param>
    /// <param name="projectName"></param>
    /// <returns></returns>
    private static Credential FindContentCredential(List<CredentialNode> list, string contentName, string projectName)
    {
        foreach (var credentialNode in list)
        {
            if(credentialNode.IsMatch(contentName, projectName))
            {
                return credentialNode.Credential;
            }
        }

        return null; //No match found
    }

    /// <summary>
    /// Adds a credential to the workbooks list
    /// </summary>
    /// <param name="workbookName"></param>
    /// <param name="projectName"></param>
    /// <param name="credentialId"></param>
    /// <param name="password"></param>
    /// <param name="isEmbedded"></param>
    public void AddWorkbookCredential(string workbookName, string projectName, string credentialId, string password, bool isEmbedded)
    {
        _workbookCredentials.Add(new CredentialNode(
            workbookName, projectName, new Credential(credentialId, password, isEmbedded)));
    }

    /// <summary>
    /// Adds a credential to the workbooks list
    /// </summary>
    /// <param name="datasourceName"></param>
    /// <param name="projectName"></param>
    /// <param name="credentialId"></param>
    /// <param name="password"></param>
    /// <param name="isEmbedded"></param>
    public void AddDatasourceCredential(string datasourceName, string projectName, string credentialId, string password, bool isEmbedded)
    {
        _datasourceCredentials.Add(new CredentialNode(
            datasourceName, projectName, new Credential(credentialId, password, isEmbedded)));
    }


    private class CredentialNode
    {
        public Credential Credential
        {
            get
            {
                return _credential; 
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contentName"></param>
        /// <param name="projectName"></param>
        /// <param name="credential"></param>
        public CredentialNode(string contentName, string projectName, Credential credential)
        {
            _credential = credential;
            _contentName = contentName;
            _projectName = projectName;
        }


        private readonly Credential _credential;
        private readonly string _projectName;
        private readonly string _contentName;
        public bool IsMatch(string contentName, string projectName)
        {
            return (projectName == _projectName) && (contentName == _contentName);
        }
    }
}
