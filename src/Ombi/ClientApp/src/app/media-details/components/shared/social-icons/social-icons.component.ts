import { Component, Input, Output, EventEmitter } from "@angular/core";
import { RequestType } from "../../../../interfaces";
@Component({
    selector: "social-icons",
    templateUrl: "./social-icons.component.html",
    styleUrls: ["./social-icons.component.scss"]
})
export class SocialIconsComponent {
    @Input() homepage: string;
    @Input() theMoviedbId: number;
    @Input() hasTrailer: boolean;
    @Input() imdbId: string;
    @Input() tvdbId: string;
    @Input() twitter: string;
    @Input() facebook: string;
    @Input() instagram: string;
    @Input() available: boolean;
    @Input() plexUrl: string;
    @Input() embyUrl: string;
    @Input() jellyfinUrl: string;
    @Input() doNotAppend: boolean;
    @Input() type: RequestType;

    @Input() isAdmin: boolean;
    @Input() canShowAdvanced: boolean;
    @Input() has4KRequest: boolean;

    @Output() openTrailer: EventEmitter<any> = new EventEmitter();
    @Output() onAdvancedOptions: EventEmitter<any> = new EventEmitter();
    @Output() onReProcessRequest: EventEmitter<any> = new EventEmitter();
    @Output() onReProcess4KRequest: EventEmitter<any> = new EventEmitter();

    public RequestType = RequestType;


    public openDialog() {
        this.openTrailer.emit();
    }

    public openAdvancedOptions() {
        this.onAdvancedOptions.emit();
    }

    public reProcessRequest(is4K: boolean) {
        if (is4K) {
            this.onReProcess4KRequest.emit();
        } else {
            this.onReProcessRequest.emit();
        }
    }
}
