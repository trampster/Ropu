import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { Router } from "@angular/router";
import { NgForm } from '@angular/forms';
import { AuthService } from '../auth.service';

@Component({
    selector: 'create-group-component',
    templateUrl: './create-group.component.html'
})
export class CreateGroupComponent
{
    invalidGroup: boolean;

    constructor(
        private router: Router, 
        private http: HttpClient, 
        @Inject('BASE_URL') private baseUrl: string,
        private authService: AuthService) 
    {
    }

    create(form: NgForm)
    {
        let groupDetails = JSON.stringify(form.value);
        this.http.post(this.baseUrl + 'api/groups/create', groupDetails, {
            headers: new HttpHeaders({
                "Content-Type": "application/json"
            })
        }).subscribe(response =>
        {
            this.invalidGroup = false;
        }, err =>
        {
            console.error(err);
            this.invalidGroup = true;
        });
    }

}
