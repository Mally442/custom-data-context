using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Transactions;
using BLL.Infrastructure.Commands;

namespace BLL.DataAccess.DataModel
{
	public partial interface IBillingDataContext : IDisposable
	{
		/// <summary>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		T CreateEntity<T>() where T : BaseEntity;

		/// <summary>
		/// Adds an instance of a BaseEntity.
		/// </summary>
		/// <typeparam name="T">The type of entity being added</typeparam>
		/// <param name="item">The entity</param>
		/// <returns></returns>
		T Add<T>(T item, string user = null) where T : BaseEntity;

		/// <summary>
		/// Gets an instance of a BaseEntity by id.
		/// </summary>
		/// <typeparam name="T">The type of entity being added</typeparam>
		/// <param name="item">The entity</param>
		/// <returns></returns>
		T Get<T>(object[] keys) where T : BaseEntity;

		/// <summary>
		/// Adds an instance of a BaseEntity.
		/// </summary>
		/// <typeparam name="T">The type of entity being added</typeparam>
		/// <param name="item">The entity</param>
		/// <returns></returns>
		T Update<T>(T item, string user = null) where T : BaseEntity;

		/// <summary>
		/// Saves all changes on the context to the database.
		/// </summary>
		/// <returns></returns>
		int Save();
	}

	public partial class BillingDataContext : IBillingDataContext
	{
		/// <summary>
		/// Adds an instance of a BaseEntity.
		/// </summary>
		/// <typeparam name="T">The type of entity being added</typeparam>
		/// <param name="item">The entity</param>
		/// <returns></returns>
		public T Add<T>(T item, string user = null) where T : BaseEntity
		{
			if (item == null)
			{
				var entity = default(T);
				this.AddLoggableUser(entity, user);
				return entity;
			}

			var entry = this.Entry<T>(item);

			if (entry.State == EntityState.Detached)
			{
				item = this.Set<T>().Add(item);
			}
			this.AddLoggableUser(item, user);
			return item;
		}

		public T Get<T>(object[] keys) where T : BaseEntity
		{
			return this.Set<T>().Find(keys); 
		}

		/// <summary>
		/// Updates an instance of a BaseEntity.
		/// </summary>
		/// <typeparam name="T">The type of entity being added</typeparam>
		/// <param name="item">The entity</param>
		/// <returns></returns>
		public T Update<T>(T item, string user = null) where T : BaseEntity
		{
			if (item == null)
			{
				return default(T);
			}
			var entry = this.Entry<T>(item);
			item = this.Set<T>().Attach(entry.Entity);
			this.AddLoggableUser(item, user);
			entry.State = EntityState.Modified;
			
			return item;
		}

		/// <summary>
		/// Creates an instance of a BaseEntity.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T CreateEntity<T>() where T : BaseEntity
		{
			return Activator.CreateInstance<T>();
		}

		/// <summary>
		/// Saves all changes on the context to the database.
		/// </summary>
		/// <returns></returns>
		public int Save()
		{
			List<DbEntityEntry> entries = this.ChangeTracker.Entries().ToList();
			ICommandExecutor commandExecutor = Resolver.Get<ICommandExecutor>();

			// Make sure we validate any of the object changes
			foreach (DbEntityEntry entry in entries)
			{
				BaseEntity baseEntity = entry.Entity as BaseEntity;
				if (baseEntity != null && (entry.State == EntityState.Added || entry.State == EntityState.Modified))
				{
					baseEntity.Validate();
					this.AddLoggableDate(entry.Entity);
				}
			}

			using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required))
			{
				int result = this.SaveChanges();

				scope.Complete();
				return result;
			}
		}

		private void AddLoggableDate(object entity)
		{
			ILoggable loggable = entity as ILoggable;
			if (loggable != null && !loggable.IgnoreLoggableFieldsOnCommit)
			{
				DateTime now = DateContext.UTCNow();
				if (loggable.CreatedOn == default(DateTime))
				{
					loggable.CreatedOn = now;
				}
				loggable.ModifiedOn = now;
			}
		}

		private void AddLoggableUser(object entity, string user)
		{
			if(!user.IsNullOrWhitespace())
			{
				ILoggable loggable = entity as ILoggable;
				if (loggable != null && !loggable.IgnoreLoggableFieldsOnCommit)
				{
					if (loggable.CreatedBy == default(string))
					{
						loggable.CreatedBy = user;
					}
					loggable.ModifiedBy = user;
				}
			}
		}
	}
}
