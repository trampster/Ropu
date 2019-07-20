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

    hasRole(user: UserInfo, role: string): boolean
    {
        let roles = user.roles;
        for (var i = 0; i < roles.length; i++) 
        {
            if (roles[i] == role) 
            {
                return true;
            }
        }
        return false;
    }

    ngOnInit()
    {
        this.a.params.subscribe(params =>
        {
            this.id = this.a.snapshot.params.userid;
            this.http.get<UserInfo>(this.baseUrl + 'api/Users/' + this.id).subscribe(result =>
            {
                this.user = new FullUserInfo();
                this.user.name = result.name;
                this.user.email = result.email;
                this.user.id = result.id;
                this.user.imageHash = result.imageHash;
                this.user.isAdmin = this.hasRole(result, "Admin");
                this.user.isUser = this.hasRole(result, "User");

                this.loaded = true;
                this.nameFormData = new FormGroup({
                    name: new FormControl(this.user.name),
                    email: new FormControl(this.user.email),
                    isAdmin: new FormControl(this.user.isAdmin),
                    isUser: new FormControl(this.user.isUser),
                });
            }, error => console.error(error));
        });
    }

    editUser(user): void
    {
        this.user.name = user.name;
        console.debug("edit User" + user.name);

        let userInfo = new UserInfo();
        userInfo.id = this.user.id;
        userInfo.name = user.name;
        userInfo.email = user.email;
        userInfo.imageHash = this.user.imageHash; //TODO: change this when we add editing the image
        userInfo.roles = [];
        if (user.isAdmin)
        {
            userInfo.roles.push("Admin");
        }
        if (user.isUser)
        {
            userInfo.roles.push("User");
        }

        console.error(userInfo);

        this.http.post<UserInfo>(this.baseUrl + 'api/Users/Edit', JSON.stringify(userInfo),
            {
                headers: new HttpHeaders(
                    {
                        "Content-Type": "application/json"
                    })
            }).subscribe(result => 
            {
                if (this.authService.getUser().id == this.user.id)
                {
                    this.authService.refresh();
                }
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
                this.user.imageHash = result.hash;
            }, error => console.error(error));
    }
}

class ImageResult
{
    hash: string;
}

class UserInfo
{
    id: number;
    name: string;
    imageHash: string;
    email: string;
    roles: string[];
}

class FullUserInfo
{
    id: number;
    name: string;
    imageHash: string;
    email: string;
    isAdmin: boolean;
    isUser: boolean;
}
