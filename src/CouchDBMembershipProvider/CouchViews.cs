using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DreamSeat;
using DreamSeat.Support;

namespace CouchDBMembershipProvider
{
    public class CouchViews
    {
        public const string DESIGN = "_design/";
        public const string AUTH_VIEW_ID = "auth";
        public const string AUTH_VIEW_NAME_BY_USERNAME = "byUserName";
        public const string AUTH_VIEW_NAME_BY_Email_AND_APPNAME = "byEmailAndAppName";
        public const string AUTH_VIEW_NAME_BY_Email_AND_APPNAME_AND_ROWCOUNT = "byEmailAndAppNameAndRowCount";
        public const string AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME = "byUserNameAndAppName";
        public const string AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME_EXISTS = "byUserNameAndAppNameExists";

        private CouchDatabase _DB;

        public CouchViews()
        {
            _DB = CouchDBClient.Instance.GetDatabase(CouchSettings.Database);
        }

        public void CreateViews()
        {       
            if(!_DB.DocumentExists(string.Format("{0}{1}",DESIGN, AUTH_VIEW_ID)))
                CreateAuthViews();
        }
        
        private void CreateAuthViews()
        {            
            CouchDesignDocument authDD = new CouchDesignDocument("auth");
            authDD.Views.Add(AUTH_VIEW_NAME_BY_USERNAME, new CouchView(CouchViewFunctions.byUserNameMap));
            authDD.Views.Add(AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME, new CouchView(CouchViewFunctions.byUserNameAndAppName));
            authDD.Views.Add(AUTH_VIEW_NAME_BY_USERNAME_AND_APPNAME_EXISTS, new CouchView(CouchViewFunctions.byUserNameAndAppNameExists));
            authDD.Views.Add(AUTH_VIEW_NAME_BY_Email_AND_APPNAME, new CouchView(CouchViewFunctions.byEmailAndAppName));
            authDD.Views.Add(AUTH_VIEW_NAME_BY_Email_AND_APPNAME_AND_ROWCOUNT, new CouchView(CouchViewFunctions.byEmailAndAppNameAndRowCount));
            _DB.CreateDocument(authDD);
            
        }

        public static ViewOptions ViewOptionsForDualKeyViewSelectSingle(string userNameOrEmail, string appName)
        {
            ViewOptions vo = new ViewOptions();
            vo.Key = new KeyOptions(new string[] { userNameOrEmail, appName });

            return vo;
        }
    }
}
