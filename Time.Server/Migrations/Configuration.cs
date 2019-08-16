using JetBrains.Annotations;
using NFive.SDK.Server.Migrations;
using NFive.Time.Server.Storage;

namespace NFive.Time.Server.Migrations
{
	[UsedImplicitly]
	public sealed class Configuration : MigrationConfiguration<StorageContext> { }
}
