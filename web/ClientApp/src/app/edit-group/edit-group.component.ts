import { Component, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../auth.service';
import { FormGroup, FormControl } from '@angular/forms';
import {Location} from '@angular/common';

@Component({
    selector: 'app-group-component',
    templateUrl: './edit-group.component.html'
})
export class GroupUserComponent
{
    public id: string;
    public group: Group;
    loaded: boolean;
    editable: boolean;
    nameFormData: FormGroup;

    constructor(
        private a: ActivatedRoute, 
        private http: HttpClient, 
        @Inject('BASE_URL') private baseUrl: string, 
        private authService: AuthService,
        private location: Location)
    {
        this.loaded = false;
    }

    ngOnInit()
    {
        this.a.params.subscribe(params =>
        {
            this.id = this.a.snapshot.params.userid;
            this.http.get<Group>(this.baseUrl + 'api/Groups/' + this.id).subscribe(result =>
            {
                this.group = result;

                this.loaded = true;
                this.nameFormData = new FormGroup({
                    name: new FormControl(this.group.name),
                });
            }, error => console.error(error));
        });
    }

    editGroup(group): void
    {
        this.group.name = group.name;

        this.http.post<Group>(this.baseUrl + 'api/Groups/Edit', JSON.stringify(group),
            {
                headers: new HttpHeaders(
                {
                    "Content-Type": "application/json"
                })
            }).subscribe(result => 
            {
                this.location.back();
            }, error => console.error(error));
    }

    onFileChanged(event) : void
    {
        const file = event.target.files[0];
        const uploadData = new FormData();
        uploadData.append('image', file, "name");
        this.http.post<ImageResult>(this.baseUrl + 'api/Image/Upload', uploadData)
            .subscribe(result => 
            {
                console.info("Upload Result" + result.hash);
                this.group.imageHash = result.hash;
            }, error => console.error(error));
    }
}

class ImageResult
{
    hash: string;
}

class Group
{
    id: number;
    name: string;
    imageHash: string;
}