import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from "@angular/router";

@Component({
    selector: 'app-groups',
    templateUrl: './groups.component.html'
})
export class GroupsComponent
{
    public groups: Group[];

    constructor(
        http: HttpClient, 
        @Inject('BASE_URL') baseUrl: string, 
        private router: Router)
    {
        http.get<Group[]>(baseUrl + 'api/Groups/Groups').subscribe(result =>
        {
            this.groups = result;
        }, error => console.error(error));
    }

    add(): void
    {
        this.router.navigate(["/creategroup"]);
    }

    showGroup(user: Group) 
    {
        this.router.navigate(['/group/' + user.id]);
    }
}

interface Group
{
    name: string;
    id: number;
}
