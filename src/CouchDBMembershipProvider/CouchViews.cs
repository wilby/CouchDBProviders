using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wcjj.CouchClient;

namespace CouchDBMembershipProvider
{
    public class CouchViews
    {        
        public const string DESIGN_DOC_AUTH = "auth";
        public const string AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME = "byUserNameAndAppName";
        public const string AUTH_VIEW_NAME_BY_Email_AND_APPNAME = "byEmailAndAppName";
        public const string AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME_EXISTS = "byUserNameAndAppNameExists";
        public const string AUTH_VIEW_NAME_ALL_USERS_FOR_APP = "allUsersForAppName";
        public const string AUTH_VIEW_USERS_ONLINE = "userIsOnline";
        public const string AUTH_VIEW_NAME_BY_EMAIL_AND_APPNAME_VALUE_IS_USERNAME_ONLY = "byEmailAndAppNameValueIsUsernameOnly";

        public void CreateViews(Client client)
        {
            var designDoc = new CouchDesignDocument("auth");
            if (!client.DocumentExists(designDoc.Id))
            {
                designDoc.Views.Add("byUserName", new Dictionary<string, string>() {
                {"map", CouchViewFunctions.byUserNameMap }});

                designDoc.Views.Add("byUserNameAndAppName", new Dictionary<string, string>() {
                {"map", CouchViewFunctions.byUserNameAndAppName }});

                designDoc.Views.Add("byUserNameAndAppNameExists", new Dictionary<string, string>() {
                {"map", CouchViewFunctions.byUserNameAndAppNameExists } });

                designDoc.Views.Add("byEmailAndAppName", new Dictionary<string, string>() {
                {"map", CouchViewFunctions.byEmailAndAppName }});

                designDoc.Views.Add("byEmailAndAppNameAndRowCount", new Dictionary<string, string>() {
                {"map", CouchViewFunctions.byEmailAndAppNameAndRowCount }});

                designDoc.Views.Add("allUsersForAppName", new Dictionary<string, string>() {
                {"map", CouchViewFunctions.allUsersForAppName }});

                designDoc.Views.Add("userIsOnline", new Dictionary<string, string>() {
                {"map", CouchViewFunctions.userIsOnline }, { "reduce", CouchViewFunctions.userIsOnlineReduce } });

                designDoc.Views.Add("byEmailAndAppNameValueIsUsernameOnly", new Dictionary<string, string>() {
                {"map", CouchViewFunctions.byEmailAndAppNameValueIsUsernameOnly } });

                client.SaveDocument<CouchDesignDocument>(designDoc);
            }
        }
    }
}
