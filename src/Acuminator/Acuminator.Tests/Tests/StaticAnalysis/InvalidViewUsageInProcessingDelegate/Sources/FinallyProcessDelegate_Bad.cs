﻿using PX.Data;
using PX.SM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acuminator.Tests.Tests.StaticAnalysis.InvalidViewUsageInProcessingDelegate.Sources
{
    public class UsersProcess : PXGraph<UsersProcess>
    {
        public PXCancel<Users> Cancel;

        public PXProcessing<Users, Where<Users.guest, Equal<False>>> OurUsers;

        public PXSetup<BlobProviderSettings> BolbSettings;

        public PXSelect<Users> AllUsers;

        public UsersProcess()
        {
            OurUsers.SetProcessAllCaption("Process users");
            OurUsers.SetProcessCaption("Process user");

            OurUsers.SetProcessDelegate(ProcessItem, FinallyProcess);
            OurUsers.SetProcessDelegate(
                delegate (UsersProcess graph, Users user)
                {
                    graph.AllUsers.Update(user);
                    graph.Persist();
                },
                delegate (UsersProcess graph)
                {
                    graph.AllUsers.Select().Clear();
                }
            );
            OurUsers.SetProcessDelegate(
                (UsersProcess graph, Users user) =>
                {
                    graph.AllUsers.Update(user);
                    graph.Persist();
                },
                (UsersProcess graph) =>
                {
                    graph.AllUsers.Select().Clear();
                });
            OurUsers.SetProcessDelegate(
                (UsersProcess graph, Users user) => graph.AllUsers.Select().Clear(),
                (UsersProcess graph) => graph.AllUsers.Select().Clear());
        }

        private static void ProcessItem(Users user)
        {
            var processingGraph = PXGraph.CreateInstance<UsersProcess>();

            processingGraph.AllUsers.Update(user);
            processingGraph.Persist();
        }

        private static void FinallyProcess(UsersProcess graph)
        {
            graph.AllUsers.Select().Clear();
        }
    }
}
