using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wcjj.CouchClient;

namespace CouchDBProviders
{
    /// <summary>
    /// A data class the contains the design documents and views that are required for CouchDBProviders.
    /// </summary>
    public class CouchViews
    {        
        public const string DESIGN_DOC_AUTH = "auth";
        public const string MEMVIEW_BY_USERNAME_AND_APPNAME = "byUserNameAndAppName";
        public const string MEMVIEW_BY_Email_AND_APPNAME = "byEmailAndAppName";
        public const string MEMVIEW_BY_USERNAME_AND_APPNAME_EXISTS = "byUserNameAndAppNameExists";
        public const string MEMVIEW_ALL_USERS_FOR_APP = "allUsersForAppName";
        public const string MEMVIEW_USERS_ONLINE = "userIsOnline";
        public const string MEMVIEW_BY_EMAIL_AND_APPNAME_VALUE_IS_USERNAME_ONLY = "byEmailAndAppNameValueIsUsernameOnly";
        public const string ROLEVIEW_BY_ROLE_NAME_AND_APPNAME = "byRoleAndAppName";
        public const string ROLEVIEW_BY_USER_ROLES = "byUserRoles";
        public const string ROLEVIEW_BY_USERS_WITH_ROLES = "byUsersWithRoles";
        public const string ROLEVIEW_BY_USERNAMES_WITH_ROLE = "byUserNamesWithRole";
        public const string ROLEVIEW_ROLES_BY_APPNAME = "rolesByApplicationName";

        CouchDesignDocument designDoc;

        public CouchViews()
        {
            designDoc = new CouchDesignDocument(DESIGN_DOC_AUTH);
        }

        /// <summary>
        /// Create the design document that contains all the required views for CouchDBProviders.
        /// This method checks to see if the document already exists before attempting to create it.
        /// </summary>
        /// <param name="client"></param>
        public void CreateViews(Client client)
        {            
            if (!client.DocumentExists(designDoc.Id))
            {
                SetupMembershipViews();
                SetupRoleViews();
                client.SaveDocument<CouchDesignDocument>(designDoc);
            }
        }

        private void SetupMembershipViews()
        {            
            //designDoc.Views.Add("byUserName", new Dictionary<string, string>() {
            //{"map", CouchViewFunctions.byUserNameMap }});

            designDoc.Views.Add(MEMVIEW_BY_USERNAME_AND_APPNAME, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.byUserNameAndAppName }});

            designDoc.Views.Add(MEMVIEW_BY_USERNAME_AND_APPNAME_EXISTS, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.byUserNameAndAppNameExists } });

            designDoc.Views.Add(MEMVIEW_BY_Email_AND_APPNAME, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.byEmailAndAppName }});

            //designDoc.Views.Add("byEmailAndAppNameAndRowCount", new Dictionary<string, string>() {
            //{"map", CouchViewFunctions.byEmailAndAppNameAndRowCount }});

            designDoc.Views.Add(MEMVIEW_ALL_USERS_FOR_APP, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.allUsersForAppName }});

            designDoc.Views.Add(MEMVIEW_USERS_ONLINE, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.userIsOnline }, { "reduce", CouchViewFunctions.userIsOnlineReduce } });

            designDoc.Views.Add(MEMVIEW_BY_EMAIL_AND_APPNAME_VALUE_IS_USERNAME_ONLY, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.byEmailAndAppNameValueIsUsernameOnly } });            
        }

        private void SetupRoleViews()
        {
            designDoc.Views.Add(ROLEVIEW_BY_ROLE_NAME_AND_APPNAME, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.byRoleAndAppName }});

            designDoc.Views.Add(ROLEVIEW_BY_USER_ROLES, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.byUserRoles }});

            designDoc.Views.Add(ROLEVIEW_BY_USERS_WITH_ROLES, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.byUsersWithRoles }});

            designDoc.Views.Add(ROLEVIEW_BY_USERNAMES_WITH_ROLE, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.byUserNamesWithRole }});
            
            designDoc.Views.Add(ROLEVIEW_ROLES_BY_APPNAME, new Dictionary<string, string>() {
            {"map", CouchViewFunctions.rolesByApplicationName }});
        }
    }
}
