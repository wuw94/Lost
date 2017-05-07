﻿namespace Database.Mediator
{
    using System.Collections;

    public sealed class PartyInvitation : DataMediator
    {
        private MySQL.Table table = new MySQL.Table();

        public PartyInvitation(DataSystem dataSystem) : base(dataSystem)
        {
            wait = 0;
        }

        protected override IEnumerator UpdateTables()
        {
            yield return base.UpdateTables();

            if (dataSystem.loggedIn)
            {
                yield return query.PartyInvite.ListReceiver(table, dataSystem.profile.userName);
            }
            yield return null;
        }

        public bool Has()
        {
            return table.Length > 0;
        }

        public string Sender()
        {
            if (Has())
                return table["sender"][0];
            return "";
        }
        
        public IEnumerator RemoveInvitation(string sender)
        {
            UpdaterStop();
            table = new MySQL.Table();
            yield return query.PartyInvite.Remove(dataSystem.profile.userName, sender);
            yield return query.PartyInvite.Remove(sender, dataSystem.profile.userName);
            UpdaterStart();
        }
    }
}