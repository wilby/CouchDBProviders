CouchDBProviders - Asp.Net CouchDB Membership and Role Providers

Example Connection Strings:

<connectionStrings>
	<add name="CouchServer" connectionString="Host=localhost;Port=5984;Database=membership;UserName=appuser;Password=p@ssw0rd;Ssl=false" />
	<add name="ExampleProxy" connectionString="Host=127.0.0.1;Port=9999;UserName=user;Password=pass;"/>
</connectionStrings>

Example Membership Configuration:

<membership defaultProvider="CouchDBMembership">
      <providers>
        <clear />
        <add name="CouchDBMembership" applicationName="/" 
		type="CouchDBProviders.MembershipProvider, CouchDBProviders" 
		connectionStringName="CouchServer" proxyConnectionStringName="" enablePasswordRetrieval="true"
	                enablePasswordReset="true" requiresQuestionAndAnswer="false" maxInvalidPasswordAttempts="5" 
		passwordAttemptWindow="10" passwordFormat="Hashed" requiresUniqueEmail="true" 
		minRequiredPasswordLength="8" minRequiredNonAlphanumericCharacters="0" userIsOnlineTimeWindow="15" />
      </providers>
</membership>    

Example Role Configuration:

<roleManager enabled="true" defaultProvider="CouchDbRole">
      <providers>
        <clear />
        <add name="CouchDbRole" applicationName="/" 
		connectionStringName="CouchServer" 
		proxyConnectionStringName="" 
		type="CouchDBProviders.RoleProvider, CouchDBProviders"  />
      </providers>
</roleManager>