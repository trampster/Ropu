import { AuthGuard } from './guards/auth-guard.service';
import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { UsersComponent } from './users/users.component';
import { UserComponent } from './user/user.component';
import { GroupsComponent } from './groups/groups.component';
import { LoginComponent } from './login/login.component';
import { CreateUserComponent } from './create-user/create-user.component';
import { CreateGroupComponent } from './create-group/create-group.component';
import { EditUserComponent } from './edit-user/edit-user.component';

import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './Intercepters/AuthInterceptor';

@NgModule({
    declarations: [
        AppComponent,
        NavMenuComponent,
        HomeComponent,
        UsersComponent,
        GroupsComponent,
        LoginComponent,
        CreateUserComponent,
        UserComponent,
        EditUserComponent,
        CreateGroupComponent,
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        HttpClientModule,
        FormsModule,
        ReactiveFormsModule,
        RouterModule.forRoot([
            { path: '', component: HomeComponent, pathMatch: 'full' },
            { path: 'users', component: UsersComponent, canActivate: [AuthGuard] },
            { path: 'user/:userid', component: UserComponent, canActivate: [AuthGuard] },
            { path: 'groups', component: GroupsComponent, canActivate: [AuthGuard] },
            { path: 'login', component: LoginComponent },
            { path: 'createuser', component: CreateUserComponent },
            { path: 'edituser/:userid', component: EditUserComponent, canActivate: [AuthGuard] },
            { path: 'creategroup', component: CreateGroupComponent, canActivate: [AuthGuard] },
        ])
    ],
    providers: [
        AuthGuard,
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true
        }
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
