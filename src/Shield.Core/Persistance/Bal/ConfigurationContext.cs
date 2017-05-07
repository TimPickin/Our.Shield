﻿using Newtonsoft.Json;
using System;
using Umbraco.Core;

namespace Shield.Core.Persistance.Bal
{
    /// <summary>
    /// The Configuration Context.
    /// </summary>
    public static class ConfigurationContext
    {
        /// <summary>
        /// Reads a Configuration from the database.
        /// </summary>
        /// <param name="id">
        /// The id of the configuration.
        /// </param>
        /// <param name="type">
        /// The type of configuration to return;
        /// </param>
        /// <returns>
        /// The Configuration as the desired type.
        /// </returns>
        public static Models.Configuration Read(string id, Type type)
        {
            var db = ApplicationContext.Current.DatabaseContext.Database;
            var record = db.SingleOrDefault<Dal.Configuration>((object)id);

            if (record == null || string.IsNullOrEmpty(record.Value))
            {
                return null;
            }

            var config = JsonConvert.DeserializeObject(record.Value, type) as Models.Configuration;

            return config;
        }

        /// <summary>
        /// Writes a Configuration to the database.
        /// </summary>
        /// <param name="id">
        /// The id of Configuration to write.
        /// </param>
        /// <param name="config">
        /// The configuration to write to the database
        /// </param>
        /// <returns>
        /// If successfull, returns true, otherwise false.
        /// </returns>
        public static bool Write(string id, Models.Configuration config)
        {
            var db = ApplicationContext.Current.DatabaseContext.Database;
            var record = new Dal.Configuration
            {
                Id = id,
                LastModified = DateTime.UtcNow,
                Value = JsonConvert.SerializeObject(config)
            };

            if (db.Exists<Dal.Configuration>(id))
            {
                db.Update(record);
            }
            else
            {
                db.Insert(record);
            }

            return true;
        }
    }
}
