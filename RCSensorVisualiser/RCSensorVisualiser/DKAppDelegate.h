//
//  DKAppDelegate.h
//  RCDistanceVisualiser
//
//  Created by Daniel Kennett on 05/01/2013.
//  Copyright (c) 2013 Daniel Kennett. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "DKRCCarDistancesView.h"
#import "DKRCCarOrientationView.h"

@interface DKAppDelegate : NSObject <NSApplicationDelegate>

@property (assign) IBOutlet NSWindow *window;
@property (weak) IBOutlet DKRCCarDistancesView *carView;
@property (weak) IBOutlet DKRCCarOrientationView *orientationView;

- (IBAction)connectToEndpoint:(id)sender;

@end
