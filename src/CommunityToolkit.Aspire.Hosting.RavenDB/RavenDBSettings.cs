
using Raven.Client.Documents;
using System.Security.Cryptography.X509Certificates;

namespace CommunityToolkit.Aspire.RavenDB.Client;

/// <summary>
/// Provides the client configuration settings for connecting to a RavenDB database.
/// </summary>
public sealed class RavenDBSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RavenDBSettings"/> class with the specified connection URLs and optional database name.
    /// </summary>
    /// <param name="urls">The URLs of the RavenDB server nodes.</param>
    /// <param name="databaseName">The optional name of the database to connect to.</param>
    public RavenDBSettings(string[] urls, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(urls);

        Urls = urls;
        DatabaseName = databaseName;
    }

    /// <summary>
    /// The URLs of the RavenDB server nodes.
    /// </summary>
    public string[] Urls { get; private set; }

    /// <summary>
    /// The path to the certificate file.
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// The password for the certificate.
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// The name of the database to connect to.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether a new database should be created.
    /// The default value is <see langword="false"/>.
    /// </summary>
    public bool CreateDatabase { get; init; } = false;

    /// <summary>
    /// Action that allows modifications of the <see cref="IDocumentStore"/>.
    /// </summary>
    public Action<IDocumentStore>? ModifyDocumentStore { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether RavenDB health check is disabled or not.
    /// The default value is <see langword="false"/>.
    /// </summary>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds for the RavenDB health check.
    /// </summary>
    public int? HealthCheckTimeout { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether OpenTelemetry tracing is disabled.
    /// The default value is <see langword="false"/>.
    /// </summary>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Retrieves the <see cref="X509Certificate2"/> used for authentication, if a certificate path is specified.
    /// </summary>
    /// <returns>An <see cref="X509Certificate2"/> instance if the <see cref="CertificatePath"/> is specified;
    /// otherwise, <see langword="null"/>.</returns>
    public X509Certificate2? GetCertificate()
    {
        if (string.IsNullOrEmpty(CertificatePath))
        {
            return null;
        }

        return new X509Certificate2(CertificatePath, CertificatePassword);
    }
}
