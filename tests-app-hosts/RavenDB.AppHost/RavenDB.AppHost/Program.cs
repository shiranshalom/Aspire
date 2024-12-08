using CommunityToolkit.Aspire.Hosting.RavenDB;

var builder = DistributedApplication.CreateBuilder(args);

var serverSettings = RavenDBServerSettings.Unsecured();
serverSettings.WithLicense(license: "{\"Id\":\"d1377436-7be9-4302-b23b-841265bb9eee\",\"Name\":\"Hibernating Rhinos - Production\",\"Keys\":[\"Davz19YGMBMspCV4mejB7W1z9\",\"f5HtWIjdQt5x5YKj0LW0xcy5n\",\"gqdrXTXJ/YcwkRSnePdMKgz5I\",\"rwJnFMj2+ZA/j8d1RLSPbbMuv\",\"jOkN423pX+JPFN1RfVv12sHXj\",\"gw25wGmPgCbcMuzRWjBZpRr5q\",\"heUAII6dVEyEaLB6dIK1pABIE\",\"DNi4wJSYoSR4qKywtLi8wMScy\",\"MzQ1Fjc4OTo7PD0+nwIfIJ8CI\",\"CCfAiEgnwIjIJ8CJCCfAiUgnw\",\"ImIJ8CJyCfAiggnwIpIJ8CKiC\",\"fAisgnwIsIJ8CLSCfAi4gnwIv\",\"IJ8CMCCjAAwAAKQAAQAAYsNa\"]}");
builder.AddRavenDB("ravenServer", serverSettings).AddDatabase("TestDatabase");

builder.Build().Run();