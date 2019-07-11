import { Component, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../auth.service';
import { FormGroup, FormControl } from '@angular/forms';

@Component({
    selector: 'app-user-component',
    templateUrl: './edit-user.component.html'
})
export class EditUserComponent
{
    public id: string;
    public user: FullUserInfo;
    loaded: boolean;
    editable: boolean;
    nameFormData: FormGroup;

    constructor(private a: ActivatedRoute, private http: HttpClient, @Inject('BASE_URL') private baseUrl: string, private authService: AuthService)
    {
        this.loaded = false;
    }

    ngOnInit()
    {
        this.a.params.subscribe(params =>
        {
            this.id = this.a.snapshot.params.userid;
            this.http.get<FullUserInfo>(this.baseUrl + 'api/Users/' + this.id).subscribe(result =>
            {
                this.user = result;
                this.loaded = true;
                this.nameFormData = new FormGroup({
                    name: new FormControl(this.user.name),
                    email: new FormControl(this.user.email),
                });
            }, error => console.error(error));
        });
    }

    editUser(user): void
    {
        this.user.name = user.name;
        console.debug("edit User" + user.name);

        this.http.post<FullUserInfo>(this.baseUrl + 'api/Users/Edit', JSON.stringify(this.user),
        {
            headers: new HttpHeaders(
            {
                "Content-Type": "application/json"
            })
        }).subscribe(result => 
        {
            if(this.authService.getUser().id == this.user.id)
            {
                this.authService.refresh();
            }
        }, error => console.error(error));
    }
}

interface FullUserInfo
{
    id: number;
    name: string;
    imageHash: string;
    email: string;
}
