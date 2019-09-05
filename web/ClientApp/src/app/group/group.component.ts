import { Component, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';

@Component({
    selector: 'app-group-component',
    templateUrl: './group.component.html'
})
export class GroupComponent
{
    public id: string;
    public group: Group;
    loaded: boolean;
    editable: boolean;

    constructor(
        private a: ActivatedRoute, 
        private router: Router, 
        private http: HttpClient, 
        @Inject('BASE_URL') private baseUrl: string, 
        private authService: AuthService)
    {
        this.loaded = false;
        this.editable = true;
    }

    ngOnInit()
    {
        this.a.params.subscribe(params =>
        {
            this.id = this.a.snapshot.params.groupid;

            this.http.get<Group>(this.baseUrl + 'api/Groups/' + this.id).subscribe(result =>
            {
                this.group = result;
                this.loaded = true;
            }, error => console.error(error));
        });
    }

    editGroup(group: Group) 
    {
        this.router.navigate(['/editgroup/' + group.id]);
    }

    deleteGroup(group: Group)
    {
        if(!confirm("Are you sure to delete group " + group.name + "?")) 
        {
            return;
        }

        this.http.delete(this.baseUrl + 'api/Groups/' + this.id + '/Delete').subscribe(result =>
        {
            this.router.navigate(['/groups']);
        }, error => console.error(error));
    }

    joinGroup(group: Group)
    {
        this.http.post('api/Groups/' + this.id + '/Join/' + this.authService.getUser().id, null).subscribe(result =>
        {
        }, error => console.error(error));
    }
}

interface Group
{
    name: string;
    id: number;
    imageHash: string;
}
