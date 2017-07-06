﻿namespace Our.Shield.Core.Persistance.Data
{
    using System;
    using System.Linq;
    using Semver;
    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Persistence.Migrations;
    using Umbraco.Core.Persistence.SqlSyntax;
    using Umbraco.Core.Services;

    /// <summary>
    /// 
    /// </summary>
    internal class Migration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlSyntax"></param>
        /// <param name="migrationEntryService"></param>
        /// <param name="logger"></param>
        public void RunMigrations(ISqlSyntaxProvider sqlSyntax, IMigrationEntryService migrationEntryService, ILogger logger)
        {
            const string productName = nameof(Shield);
            var currentVersion = new SemVersion(0, 0, 0);

            var scriptsForMigration = new IMigration[]
            {
                new Migrations.EnvironmentMigration (sqlSyntax, logger),
                new Migrations.ConfigurationMigration (sqlSyntax, logger),
                new Migrations.JournalMigration (sqlSyntax, logger),
                new Migrations.DomainMigration (sqlSyntax,logger)
            };

            var migrations = ApplicationContext.Current.Services.MigrationEntryService.GetAll(productName);
            var latestMigration = migrations.OrderByDescending(x => x.Version).FirstOrDefault();

            if (latestMigration != null)
                currentVersion = latestMigration.Version;

            var targetVersion = new SemVersion(1, 0, 0);
            if (targetVersion == currentVersion)
                return;

            MigrationRunner migrationsRunner = new MigrationRunner(migrationEntryService, logger, currentVersion, targetVersion, productName, scriptsForMigration);

            try
            {
                migrationsRunner.Execute(ApplicationContext.Current.DatabaseContext.Database);
            }
            catch (Exception ex)
            {
                LogHelper.Error<Migration>("Error running Shield migration", ex);
            }
        }
    }
}