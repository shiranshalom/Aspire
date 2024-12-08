
using CommunityToolkit.Aspire.Hosting.RavenDB;

var builder = DistributedApplication.CreateBuilder(args);

/*
settings.json:
{
  "License.Eula.Accepted": true,
  "Security.Certificate.LetsEncrypt.Email": "grisha@ayende.com",
  "Setup.Mode": "LetsEncrypt",
  "Security.Certificate.Path": "/etc/ravendb/security/server.pfx",
  "ServerUrl": "https://0.0.0.0",
  "ServerUrl.Tcp": "tcp://0.0.0.0:38888",
  "ExternalIp": "0.0.0.0",
  "PublicServerUrl": "https://a.shirancontainer.development.run",
  "PublicServerUrl.Tcp": "tcp://a.shirancontainer.development.run:38888"
 }
 */

var environmentVariables = new Dictionary<string, object>
{
    {
        "RAVEN_License_Eula_Accepted",
        "true"
    },
    /*{
        "RAVEN_Security_Certificate_LetsEncrypt_Email",
        "grisha@ayende.com"
    },*/
    {
        "RAVEN_Setup_Mode",
        "LetsEncrypt"
    },
    {
        "RAVEN_Security_Certificate_Path",
        "/etc/ravendb/security/cluster.server.certificate.shirancontainer.pfx"
    },
    {
        "RAVEN_ServerUrl",
        "https://0.0.0.0"
    },
    {
        "RAVEN_ServerUrl_Tcp",
        "tcp://0.0.0.0:38888"
    },
    {
        "RAVEN_ExternalIp",
        "0.0.0.0"
    },
    {
        "RAVEN_PublicServerUrl",
        "https://a.shirancontainer.development.run"
    },
    {
        "RAVEN_PublicServerUrl_Tcp",
        "tcp://a.shirancontainer.development.run:38888"
    },
    {
        "RAVEN_Security_UnsecuredAccessAllowed",
        "None"
    },
    {
        "RAVEN_License",
        "{\"Id\":\"d1377436-7be9-4302-b23b-841265bb9eee\",\"Name\":\"Hibernating Rhinos - Production\",\"Keys\":[\"Davz19YGMBMspCV4mejB7W1z9\",\"f5HtWIjdQt5x5YKj0LW0xcy5n\",\"gqdrXTXJ/YcwkRSnePdMKgz5I\",\"rwJnFMj2+ZA/j8d1RLSPbbMuv\",\"jOkN423pX+JPFN1RfVv12sHXj\",\"gw25wGmPgCbcMuzRWjBZpRr5q\",\"heUAII6dVEyEaLB6dIK1pABIE\",\"DNi4wJSYoSR4qKywtLi8wMScy\",\"MzQ1Fjc4OTo7PD0+nwIfIJ8CI\",\"CCfAiEgnwIjIJ8CJCCfAiUgnw\",\"ImIJ8CJyCfAiggnwIpIJ8CKiC\",\"fAisgnwIsIJ8CLSCfAi4gnwIv\",\"IJ8CMCCjAAwAAKQAAQAAYsNa\"]}"
    }
};

var settings = RavenDBServerSettings.Secured(SetupMode.LetsEncrypt,
    domainUrl: "https://a.shirancontainer.development.run",
    certificatePath: "/etc/ravendb/security/cluster.server.certificate.shirancontainer.pfx",
    serverUrl: "https://0.0.0.0");
settings.WithLicense("{\"Id\":\"d1377436-7be9-4302-b23b-841265bb9eee\",\"Name\":\"Hibernating Rhinos - Production\",\"Keys\":[\"Davz19YGMBMspCV4mejB7W1z9\",\"f5HtWIjdQt5x5YKj0LW0xcy5n\",\"gqdrXTXJ/YcwkRSnePdMKgz5I\",\"rwJnFMj2+ZA/j8d1RLSPbbMuv\",\"jOkN423pX+JPFN1RfVv12sHXj\",\"gw25wGmPgCbcMuzRWjBZpRr5q\",\"heUAII6dVEyEaLB6dIK1pABIE\",\"DNi4wJSYoSR4qKywtLi8wMScy\",\"MzQ1Fjc4OTo7PD0+nwIfIJ8CI\",\"CCfAiEgnwIjIJ8CJCCfAiUgnw\",\"ImIJ8CJyCfAiggnwIpIJ8CKiC\",\"fAisgnwIsIJ8CLSCfAi4gnwIv\",\"IJ8CMCCjAAwAAKQAAQAAYsNa\"]}");

builder.AddRavenDB("ravenSecuredServer", settings/*true, environmentVariables*/)
    .WithBindMount("C:/RavenDB/Server/RavenData", "/var/lib/ravendb/data", false)
    .WithBindMount("C:/RavenDB/Server/Security", "/etc/ravendb/security", false)
    .AddDatabase("securedDatabaseResource", "TestSecuredDatabase");

builder.Build().Run();

