import { Route, Router } from "@solidjs/router";
import { MainLayout } from "@/layouts/MainLayout";
import { ApplicationDetails } from "@/pages/ApplicationDetails";
import { Applications } from "@/pages/Applications";
import { CreateApplication } from "@/pages/CreateApplication";
import { CreateScope } from "@/pages/CreateScope";
import { Dashboard } from "@/pages/Dashboard";
import { EditApplication } from "@/pages/EditApplication";
import { EditScope } from "@/pages/EditScope";
import { ScopeDetails } from "@/pages/ScopeDetails";
import { Scopes } from "@/pages/Scopes";

export default function App() {
  return (
    <Router base="/admin">
      <Route component={MainLayout}>
        <Route path="/" component={Dashboard} />
        <Route path="/applications" component={Applications} />
        <Route path="/applications/create" component={CreateApplication} />
        <Route path="/applications/:id" component={ApplicationDetails} />
        <Route path="/applications/:id/edit" component={EditApplication} />
        <Route path="/scopes" component={Scopes} />
        <Route path="/scopes/create" component={CreateScope} />
        <Route path="/scopes/:id" component={ScopeDetails} />
        <Route path="/scopes/:id/edit" component={EditScope} />
      </Route>
    </Router>
  );
}
